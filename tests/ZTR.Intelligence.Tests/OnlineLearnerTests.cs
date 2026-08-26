using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class OnlineLearnerTests
{
    private readonly MlpNetwork _network;
    private readonly OnlineLearner _learner;

    public OnlineLearnerTests()
    {
        _network = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 42);
        _learner = new OnlineLearner(_network, initialLearningRate: 0.01, decayRate: 0.001);
    }

    [Fact]
    public void Train_SingleSample_ReturnsNonNegativeLoss()
    {
        var sample = CreateRandomSample(16, 8);

        double loss = _learner.Train(sample);

        Assert.True(loss >= 0.0);
    }

    [Fact]
    public void Train_SingleSample_IncrementsUpdateCount()
    {
        var sample = CreateRandomSample(16, 8);

        int before = _learner.UpdateCount;
        _learner.Train(sample);
        int after = _learner.UpdateCount;

        Assert.Equal(before + 1, after);
    }

    [Fact]
    public void Train_SingleSample_LearningRateDecays()
    {
        double initial = _learner.CurrentLearningRate;

        _learner.Train(CreateRandomSample(16, 8));
        double afterOne = _learner.CurrentLearningRate;

        _learner.Train(CreateRandomSample(16, 8));
        double afterTwo = _learner.CurrentLearningRate;

        Assert.True(afterOne < initial);
        Assert.True(afterTwo < afterOne);
    }

    [Fact]
    public void Train_NullSample_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _learner.Train(null!));
    }

    [Fact]
    public void Train_WrongFeatureSize_ThrowsArgumentException()
    {
        var sample = new MlpTrainingSample
        {
            Features = new double[5],
            Target = new double[8]
        };

        Assert.Throws<ArgumentException>(() => _learner.Train(sample));
    }

    [Fact]
    public void Train_WrongTargetSize_ThrowsArgumentException()
    {
        var sample = new MlpTrainingSample
        {
            Features = new double[16],
            Target = new double[3]
        };

        Assert.Throws<ArgumentException>(() => _learner.Train(sample));
    }

    [Fact]
    public void TrainBatch_MultipleSamples_ReturnsAverageLoss()
    {
        var batch = new MlpTrainingSample[5];
        for (int i = 0; i < 5; i++)
        {
            batch[i] = CreateRandomSample(16, 8);
        }

        double loss = _learner.TrainBatch(batch);

        Assert.True(loss >= 0.0);
    }

    [Fact]
    public void TrainBatch_EmptyBatch_ReturnsZero()
    {
        double loss = _learner.TrainBatch(Array.Empty<MlpTrainingSample>());

        Assert.Equal(0.0, loss);
    }

    [Fact]
    public void TrainBatch_IncrementsUpdateCount()
    {
        var batch = new MlpTrainingSample[3];
        for (int i = 0; i < 3; i++)
        {
            batch[i] = CreateRandomSample(16, 8);
        }

        int before = _learner.UpdateCount;
        _learner.TrainBatch(batch);

        Assert.Equal(before + 1, _learner.UpdateCount);
    }

    [Fact]
    public void ResetLearningRate_ResetsToInitial()
    {
        _learner.Train(CreateRandomSample(16, 8));
        _learner.Train(CreateRandomSample(16, 8));
        Assert.True(_learner.CurrentLearningRate < 0.01);

        _learner.ResetLearningRate();

        Assert.Equal(0.01, _learner.CurrentLearningRate, 10);
        Assert.Equal(0, _learner.UpdateCount);
    }

    [Fact]
    public void Train_ConvergesOnSimpleTask()
    {
        var network = new MlpNetwork(inputSize: 4, hiddenSize: 16, outputSize: 1, seed: 123);
        var learner = new OnlineLearner(network, initialLearningRate: 0.1, decayRate: 0.0001);

        var trainingData = new List<MlpTrainingSample>();
        for (int i = 0; i < 200; i++)
        {
            double x = i / 200.0;
            var features = new double[] { x, x * x, 1.0 - x, Math.Sqrt(x) };
            var target = new double[] { x * 0.7 + 0.15 };
            trainingData.Add(new MlpTrainingSample
            {
                Features = features,
                Target = target,
                Timestamp = DateTime.Now
            });
        }

        double initialLoss = 0;
        for (int i = 0; i < trainingData.Count; i++)
        {
            initialLoss += learner.Train(trainingData[i]);
        }
        initialLoss /= trainingData.Count;

        for (int epoch = 0; epoch < 50; epoch++)
        {
            foreach (var sample in trainingData)
            {
                learner.Train(sample);
            }
        }

        double finalLoss = 0;
        for (int i = 0; i < trainingData.Count; i++)
        {
            var s = trainingData[i];
            var pred = network.Predict(s.Features);
            var diff = pred[0] - s.Target[0];
            finalLoss += diff * diff;
        }
        finalLoss /= trainingData.Count;

        Assert.True(finalLoss < initialLoss,
            $"Final loss {finalLoss} should be less than initial loss {initialLoss}");
    }

    [Fact]
    public void SaveAndLoadWeights_PreservesNetworkState()
    {
        var tempPath = Path.GetTempFileName();

        var network1 = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 42);
        var learner = new OnlineLearner(network1, initialLearningRate: 0.001, decayRate: 0.0001);
        for (int i = 0; i < 10; i++)
        {
            learner.Train(CreateRandomSample(16, 8));
        }

        var features = GenerateRandomFeatures(16);
        double[] output1 = network1.Predict(features);

        for (int i = 0; i < output1.Length; i++)
        {
            Assert.False(double.IsNaN(output1[i]), $"Output[{i}] is NaN before save.");
            Assert.False(double.IsInfinity(output1[i]), $"Output[{i}] is Infinity before save.");
        }

        learner.SaveWeights(tempPath);

        var network2 = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 99);
        var learner2 = new OnlineLearner(network2);
        Assert.True(learner2.LoadWeights(tempPath));

        double[] output2 = network2.Predict(features);

        for (int i = 0; i < output1.Length; i++)
        {
            Assert.False(double.IsNaN(output2[i]), $"Output[{i}] is NaN after load.");
            Assert.Equal(output1[i], output2[i], 10);
        }

        File.Delete(tempPath);
    }

    [Fact]
    public void LoadWeights_NonExistentFile_ReturnsFalse()
    {
        Assert.False(_learner.LoadWeights("/nonexistent/path/weights.dat"));
    }

    private static MlpTrainingSample CreateRandomSample(int featureSize, int targetSize)
    {
        var random = new Random();
        var features = new double[featureSize];
        var target = new double[targetSize];

        for (int i = 0; i < featureSize; i++)
            features[i] = random.NextDouble();
        for (int i = 0; i < targetSize; i++)
            target[i] = random.NextDouble();

        return new MlpTrainingSample
        {
            Features = features,
            Target = target,
            Timestamp = DateTime.Now
        };
    }

    private static double[] GenerateRandomFeatures(int size)
    {
        var random = new Random(42);
        var features = new double[size];
        for (int i = 0; i < size; i++)
            features[i] = random.NextDouble();
        return features;
    }
}