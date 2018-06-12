using System;
using System.Collections.Generic;
using CNTK;

namespace ZeroTrain
{
    public static class CntkExt
    {
        public static IList<IList<double>> Train(this Function model, Value input)
        {
            var imageInput = model.Arguments[0];
            var labelOutput = model.Output;
            var inputDataMap = new Dictionary<Variable, Value> {{imageInput, input}};
            var outputDataMap = new Dictionary<Variable, Value> {{labelOutput, null}};
            model.Evaluate(inputDataMap, outputDataMap, nn.Device);
            return outputDataMap[labelOutput].GetDenseData<double>(labelOutput);
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
    }
}
