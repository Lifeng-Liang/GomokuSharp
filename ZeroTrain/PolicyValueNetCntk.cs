using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GomokuLib;
using CNTK;

namespace ZeroTrain
{
    public class PolicyValueNetCntk : PolicyValueNet
    {
        public static readonly string BasePath = Path.GetTempPath();
        private const string Nfeatures = "features";
        private const string Nlabels = "labels";
        private const string Nvalues = "values";
        private readonly InputVariable _inputs;
        private readonly InputVariable _labels;
        private readonly InputVariable _values;
        private readonly int _width;
        private readonly int _height;
        private readonly Function _model;
        private readonly Trainer _trainer;
        private readonly Learner _optim;

        public PolicyValueNetCntk(int width, int height) : base(width, height)
        {
            _width = width;
            _height = height;
            _inputs = nn.InputVariable(new[] { _width, _height, 4 }, Nfeatures);
            _labels = nn.InputVariable(new[] { _width * _height }, Nlabels);
            _values = nn.InputVariable(new[] { 1 }, Nvalues);

            var scaledInput = _inputs * 0.00000390625f;
            _model = CreateModel(scaledInput);

            var lossPolice = nn.CrossEntropyLoss(_model.Outputs[0], _labels);
            var lossValue = nn.MSELoss(_model.Outputs[1], _values);
            var loss = lossPolice + lossValue;
            var predPol = nn.ClassificationError(_model.Outputs[0], _labels);
            var predVal = nn.ClassificationError(_model.Outputs[1], _values);
            var pred = predPol + predVal;
            _optim = Optim.Adam(_model.Parameters());
            _trainer = Trainer.CreateTrainer(_model, loss, pred, new List<Learner> {_optim});
        }

        private static Function CreateModel(Variable input)
        {
            var conv = input.Sequence(
                nn.Conv2D(4, 32, 3),
                nn.ReLU(),
                nn.Conv2D(32, 64, 3),
                nn.ReLU(),
                nn.Conv2D(64, 128, 3),
                nn.ReLU()
                );
            var x = conv.Sequence(
                nn.Conv2D(128, 4, 1),
                nn.ReLU(),
                nn.Dense(15 * 15),
                nn.LogSoftmax()
                );
            var y = conv.Sequence(
                nn.Conv2D(128, 2, 1),
                nn.ReLU(),
                nn.Dense(64),
                nn.ReLU(),
                nn.Linear(1),
                nn.Tanh()
                );
            return nn.Combine(x.Output, y.Output);
        }

        private Value ToValue(IEnumerable<double[,,]> stateBatch)
        {
            var s = NDShape.CreateNDShape(new[] { _width, _height, 4 });
            var state = Value.CreateBatch(s, stateBatch, nn.Device);
            return state;
        }

        public override Tuple<IList<IList<double>>, IList<IList<double>>> policy_value(IEnumerable<double[,,]> stateBatch)
        {
            var state = ToValue(stateBatch);
            var result = _model.Evaluate(state);
            return Tuple.Create(result[0].exp(), result[1]);
        }

        public override Tuple<IEnumerable<Tuple<int, double>>, double> policy_value_fn(Board board)
        {
            var legalPositions = board.availables;
            var currentState = board.current_state();
            var actProbsValue = policy_value(new List<double[,,]> {currentState});
            var actProbs = GetLegalProbs(legalPositions, actProbsValue.Item1.flatten().ToList());
            return Tuple.Create(actProbs, actProbsValue.Item2[0][0]);
        }

        private static IEnumerable<Tuple<int, double>> GetLegalProbs(List<int> legalPositions, List<double> actProbs)
        {
            foreach (var pos in legalPositions)
            {
                yield return Tuple.Create(pos, actProbs[pos]);
            }
        }

        public override Tuple<double, double> train_step(IEnumerable<Tuple<double[,,], double[], double>> miniBatch, double lr)
        {
            _optim.SetLearningRateSchedule(new TrainingParameterScheduleDouble(lr));
            const string fn = "zero_train.txt";
            CreateTrainingFile(fn, miniBatch);
            var loader = GetLoader(fn);
            Train(_trainer, loader);
            var loss = _trainer.PreviousMinibatchLossAverage();
            var eval = _trainer.PreviousMinibatchEvaluationAverage();
            return Tuple.Create(loss, eval);
        }

        private void Train(Trainer trainer, MinibatchSource loader)
        {
            UnorderedMapStreamInformationMinibatchData minibatchData;
            var image = loader.StreamInfo(Nfeatures);
            var label = loader.StreamInfo(Nlabels);
            var value = loader.StreamInfo(Nvalues);
            do
            {
                minibatchData = loader.GetNextMinibatch(32, nn.Device);
                var arguments = new Dictionary<Variable, MinibatchData>
                {
                    {_inputs, minibatchData[image]},
                    {_labels, minibatchData[label]},
                    {_values, minibatchData[value]}
                };
                trainer.TrainMinibatch(arguments, nn.Device);
            } while (!minibatchData.Values.Any(a => a.sweepEnd));
        }

        private MinibatchSource GetLoader(string fileName)
        {
            var conf = new[]
            {
                new StreamConfiguration(Nfeatures, _width*_height*4),
                new StreamConfiguration(Nlabels, _width*_height),
                new StreamConfiguration(Nvalues, 1)
            };

            var minibatchSource = MinibatchSource.TextFormatMinibatchSource(
                Path.Combine(BasePath, fileName), conf,
                MinibatchSource.InfinitelyRepeat);

            return minibatchSource;
        }

        private void CreateTrainingFile(string fileName, IEnumerable<Tuple<double[,,], double[], double>> miniBatch)
        {
            var fn = Path.Combine(BasePath, fileName);
            using (var fw = new StreamWriter(new FileStream(fn, FileMode.Truncate)))
            {
                foreach (var data in miniBatch)
                {
                    fw.Write($"|{Nvalues} {data.Item3} |{Nlabels} ");
                    foreach (var label in data.Item2)
                    {
                        fw.Write($"{label} ");
                    }
                    fw.Write($"|{Nfeatures}");
                    foreach (var feature in data.Item1)
                    {
                        fw.Write($" {feature}");
                    }
                    fw.WriteLine();
                }
            }
        }

        public override void save_model(string modelFileName)
        {
            _model.Save(modelFileName);
        }
    }
}
