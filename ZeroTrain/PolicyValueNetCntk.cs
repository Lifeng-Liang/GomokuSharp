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
        private static readonly Variable Inputs = CNTKLib.InputVariable(new[] { 15, 15, 4 }, nn.Type, "features");
        private static readonly Variable Labels = CNTKLib.InputVariable(new[] { 15 * 15 }, nn.Type, "labels");
        private static readonly Variable Values = CNTKLib.InputVariable(new[] { 1 }, nn.Type, "values");
        private readonly int _width;
        private readonly int _height;
        private readonly Function _model;
        private readonly Trainer _trainer;

        public PolicyValueNetCntk(int width, int height) : base(width, height)
        {
            _width = width;
            _height = height;
            var scaledInput = CNTKLib.ElementTimes(Constant.Scalar(0.00000390625f, nn.Device), Inputs);
            _model = CreateModel(scaledInput);

            var lossPolice = CNTKLib.CrossEntropyWithSoftmax(new Variable(_model.Outputs[0]), Labels);
            var lossValue = CNTKLib.CrossEntropyWithSoftmax(new Variable(_model.Outputs[1]), Values);
            var loss = CNTKLib.Plus(lossPolice, lossValue);
            var predPol = CNTKLib.ClassificationError(new Variable(_model.Outputs[0]), Labels);
            var predVal = CNTKLib.ClassificationError(new Variable(_model.Outputs[1]), Values);
            var pred = CNTKLib.Plus(predPol, predVal);
            var optim = Optim.Adam(_model.Parameters());
            _trainer = Trainer.CreateTrainer(_model, loss, pred, new List<Learner> {optim});
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
            return Function.Combine(new List<Variable> { x.Output, y.Output });
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
            var logActProbs = _model.Train(state);
            var value = _model.Train(state);
            var actProbs = logActProbs.exp();
            return Tuple.Create(actProbs, value);
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
            const string fn = "zero_train.txt";
            CreateTrainingFile(fn, miniBatch);
            var loader = GetLoader(fn);
            Train(_trainer, loader);
            var loss = _trainer.PreviousMinibatchLossAverage();
            var eval = _trainer.PreviousMinibatchEvaluationAverage();
            // K.set_value(self.model.optimizer.lr, lr)
            return Tuple.Create(loss, eval);
        }

        private static void Train(Trainer trainer, MinibatchSource loader)
        {
            UnorderedMapStreamInformationMinibatchData minibatchData;
            var image = loader.StreamInfo("features");
            var label = loader.StreamInfo("labels");
            var value = loader.StreamInfo("values");
            do
            {
                minibatchData = loader.GetNextMinibatch(32, nn.Device);
                var arguments = new Dictionary<Variable, MinibatchData>
                {
                    {Inputs, minibatchData[image]},
                    {Labels, minibatchData[label]},
                    {Values, minibatchData[value]}
                };
                trainer.TrainMinibatch(arguments, nn.Device);
            } while (!minibatchData.Values.Any(a => a.sweepEnd));
        }

        private static MinibatchSource GetLoader(string fileName)
        {
            var conf = new[] { new StreamConfiguration("features", 15 * 15 * 4), new StreamConfiguration("labels", 15 * 15), new StreamConfiguration("values", 1) };

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
                    fw.Write($"|values {data.Item3:0.0000} |labels ");
                    foreach (var label in data.Item2)
                    {
                        fw.Write($"{label:0.0000} ");
                    }
                    fw.Write("|features");
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
