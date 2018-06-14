using System;
using System.Collections.Generic;
using CNTK;

namespace ZeroTrain
{
    public static class CntkExt
    {
        public static List<IList<IList<double>>> Evaluate(this Function model, Value input)
        {
            var imageInput = model.Arguments[0];
            var labelOutput = model.Output;
            var inputDataMap = new Dictionary<Variable, Value> {{imageInput, input}};
            var outputDataMap = new Dictionary<Variable, Value>();
            foreach (var output in model.Outputs)
            {
                outputDataMap.Add(output, null);
            }
            model.Evaluate(inputDataMap, outputDataMap, nn.Device);
            var result = new List<IList<IList<double>>>();
            foreach (var output in model.Outputs)
            {
                result.Add(outputDataMap[output].GetDenseData<double>(output));
            }
            return result;
        }
    }

    public class LossFunction
    {
        private readonly Function _func;

        public LossFunction(Function func)
        {
            _func = func;
        }

        public static Function operator +(LossFunction op1, LossFunction op2)
        {
            return CNTKLib.Plus(op1._func, op2._func);
        }

        public static implicit operator LossFunction(Function op)
        {
            return new LossFunction(op);
        }

        public static implicit operator Function(LossFunction op)
        {
            return op._func;
        }
    }

    public class InputVariable
    {
        private readonly Variable _variable;

        public InputVariable(Variable variable)
        {
            _variable = variable;
        }

        public static Function operator *(InputVariable op1, InputVariable op2)
        {
            return CNTKLib.ElementTimes(op1._variable, op2._variable);
        }

        public static Function operator *(InputVariable op1, float op2)
        {
            return CNTKLib.ElementTimes(op1._variable, Constant.Scalar(op2, nn.Device));
        }

        public static implicit operator InputVariable(Variable op)
        {
            return new InputVariable(op);
        }

        public static implicit operator Variable(InputVariable variable)
        {
            return variable._variable;
        }
    }

    public static class Optim
    {
        public static Learner SGD(IList<Parameter> parameters, double lr)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            return CNTKLib.SGDLearner(TestHelper.AsParameterVector(parameters), lrp);
        }

        public static Learner SGD(IList<Parameter> parameters, double lr, double momentum)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            var mrp = new TrainingParameterScheduleDouble(momentum);
            return CNTKLib.MomentumSGDLearner(TestHelper.AsParameterVector(parameters), lrp, mrp);
        }

        public static Learner AdamGrad(IList<Parameter> parameters, double lr)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            return CNTKLib.AdaGradLearner(TestHelper.AsParameterVector(parameters), lrp);
        }

        public static Learner AdamDelta(IList<Parameter> parameters, double lr)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            return CNTKLib.AdaDeltaLearner(TestHelper.AsParameterVector(parameters), lrp);
        }

        public static Learner Adam(IList<Parameter> parameters, double lr = 0.1, double momentum = 0.1)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            var mrp = new TrainingParameterScheduleDouble(momentum);
            return CNTKLib.AdamLearner(TestHelper.AsParameterVector(parameters), lrp, mrp);
        }

        public static void SetLearningRate(this Learner optim, double lr)
        {
            var lrp = new TrainingParameterScheduleDouble(lr);
            optim.SetLearningRateSchedule(lrp);
        }
    }

    public static class nn
    {
        public static DeviceDescriptor Device = DeviceDescriptor.CPUDevice;
        public static DataType Type = DataType.Float;

        public static Func<Variable, Function> Sequence(params Func<Variable, Function>[] layers)
        {
            return input => Sequence(input, layers);
        }

        public static Function Sequence(this Function input, params Func<Variable, Function>[] layers)
        {
            return Sequence((Variable)input, layers);
        }

        public static Function Sequence(this Variable input, params Func<Variable, Function>[] layers)
        {
            var x = input;
            foreach (var layer in layers)
            {
                x = layer(x);
            }
            return x;
        }

        public static Func<Variable, Function> Conv2D(int inChannels, int outChannels, int kernelSize, int stride = 1, int padding = 0)
        {
            var convWScale = 0.26; //TODO: why ?
            var convParams = new Parameter(new[] { kernelSize, kernelSize, inChannels, outChannels }, Type,
                CNTKLib.GlorotUniformInitializer(convWScale, -1, 2), Device);
            return input => CNTKLib.Convolution(convParams, input, new[] { 1, 1, outChannels });
        }

        public static Func<Variable, Function> ReLU()
        {
            return CNTKLib.ReLU;
        }

        public static Func<Variable, Function> Sigmoid()
        {
            return CNTKLib.Sigmoid;
        }

        public static Func<Variable, Function> Tanh()
        {
            return CNTKLib.Tanh;
        }

        public static Func<Variable, Function> MaxPool2D(int kernelSize, int stride = 2) //TODO: int padding = 0
        {
            return input => CNTKLib.Pooling(input, PoolingType.Max,
                new[] { kernelSize, kernelSize }, new[] { stride, stride }, new[] { true });
        }

        public static Func<Variable, Function> Dense(int outputDim, string outputName = "")
        {
            return input => TestHelper.Dense(input, outputDim, Device, Activation.None, outputName);
        }

        public static Func<Variable, Function> Softmax()
        {
            return CNTKLib.Softmax;
        }

        public static Func<Variable, Function> LogSoftmax()
        {
            return CNTKLib.LogSoftmax;
        }

        public static Func<Variable, Function> Linear(int outputDim, string outputName = "")
        {
            return input => TestHelper.FullyConnectedLinearLayer(input, outputDim, Device, outputName); ;
        }

        public static Func<Variable, Function> Dropout(double dropRate = 0.5)
        {
            return input => CNTKLib.Dropout(input, dropRate);
        }

        public static Func<Variable, Function> BatchNormalization(double scValue, double bValue, bool spatial)
        {
            return input =>
            {
                int outFeatureMapCount = input.Shape[0];
                var sc = new Parameter(new[] { outFeatureMapCount }, (float)scValue, Device, "");
                var b = new Parameter(new[] { outFeatureMapCount }, (float)bValue, Device, "");
                var m = new Constant(new[] { outFeatureMapCount }, 0.0f, Device);
                var v = new Constant(new[] { outFeatureMapCount }, 0.0f, Device);
                var n = Constant.Scalar(0.0f, Device);
                return CNTKLib.BatchNormalization(input, sc, b, m, v, n, spatial);
            };
        }

        public static LossFunction CrossEntropyLoss(Variable prediction, Variable labels)
        {
            return CNTKLib.CrossEntropyWithSoftmax(prediction, labels);
        }

        public static LossFunction MSELoss(Variable prediction, Variable labels)
        {
            return CNTKLib.SquaredError(prediction, labels);
        }

        public static LossFunction ClassificationError(Variable prediction, Variable labels)
        {
            return CNTKLib.ClassificationError(prediction, labels);
        }

        public static InputVariable InputVariable(NDShape shape, string name)
        {
            return CNTKLib.InputVariable(shape, Type, name);
        }

        public static Function Combine(params Variable[] funcs)
        {
            if (funcs.Length == 0)
            {
                return null;
            }
            if (funcs.Length == 1)
            {
                return funcs[0];
            }
            return Function.Combine(funcs);
        }
    }
}
