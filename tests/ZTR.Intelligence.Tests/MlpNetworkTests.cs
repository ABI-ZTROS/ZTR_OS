using ZTR.Intelligence;
using ZTR.Models;

namespace ZTR.Intelligence.Tests;

public class MlpNetworkTests
{
    [Fact]
    public void Constructor_WithValidConfig_CreatesCorrectArchitecture()
    {
        var config = new MlpConfig
        {
            InputSize = 16,
            HiddenLayerSize = 64,
            OutputSize = 8
        };

        var network = new MlpNetwork(config, seed: 42);

        Assert.Equal(16, network.InputSize);
        Assert.Equal(64, network.HiddenSize);
        Assert.Equal(8, network.OutputSize);
    }

    [Fact]
    public void Constructor_WithDirectParameters_CreatesCorrectArchitecture()
    {
        var network = new MlpNetwork(inputSize: 20, hiddenSize: 32, outputSize: 8, seed: 123);

        Assert.Equal(20, network.InputSize);
        Assert.Equal(32, network.HiddenSize);
        Assert.Equal(8, network.OutputSize);
        Assert.Equal(Math.Max(32 / 2, 16), network.HiddenSize2);
    }

    [Fact]
    public void Predict_WithCorrectInput_ReturnsCorrectOutputSize()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 64, outputSize: 8, seed: 42);
        var features = new double[16];

        double[] output = network.Predict(features);

        Assert.Equal(8, output.Length);
    }

    [Fact]
    public void Predict_AllOutputsInValidRange()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 64, outputSize: 8, seed: 42);
        var features = GenerateRandomFeatures(16);

        double[] output = network.Predict(features);

        Assert.All(output, v => Assert.InRange(v, 0.0, 1.0));
    }

    [Fact]
    public void Predict_WrongInputSize_ThrowsArgumentException()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 64, outputSize: 8, seed: 42);
        var features = new double[5];

        Assert.Throws<ArgumentException>(() => network.Predict(features));
    }

    [Fact]
    public void Predict_NullInput_ThrowsArgumentNullException()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 64, outputSize: 8, seed: 42);

        Assert.Throws<ArgumentNullException>(() => network.Predict(null!));
    }

    [Fact]
    public void Predict_DifferentInputs_ProduceDifferentOutputs()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 64, outputSize: 8, seed: 42);
        var features1 = GenerateRandomFeatures(16, seed: 100);
        var features2 = GenerateRandomFeatures(16, seed: 200);

        double[] output1 = network.Predict(features1);
        double[] output2 = network.Predict(features2);

        bool allSame = output1.Zip(output2, (a, b) => Math.Abs(a - b) < 1e-10).All(x => x);
        Assert.False(allSame);
    }

    [Fact]
    public void Predict_SameInputSameWeights_ProducesSameOutput()
    {
        var network1 = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 123);
        var network2 = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 123);

        var features = GenerateRandomFeatures(16);

        double[] output1 = network1.Predict(features);
        double[] output2 = network2.Predict(features);

        for (int i = 0; i < output1.Length; i++)
        {
            Assert.Equal(output1[i], output2[i], 10);
        }
    }

    [Fact]
    public void Predict_InferenceLatency_UnderFiveMilliseconds()
    {
        var network = new MlpNetwork(inputSize: 20, hiddenSize: 64, outputSize: 8, seed: 42);
        var features = GenerateRandomFeatures(20);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            network.Predict(features);
        }
        stopwatch.Stop();

        double avgMs = stopwatch.Elapsed.TotalMilliseconds / 1000.0;
        Assert.True(avgMs < 5.0, $"Average inference latency {avgMs:F3}ms exceeds 5ms limit.");
    }

    [Fact]
    public void GetWeights_ReturnsAllWeightMatrices()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 42);

        var (w1, b1, w2, b2, w3, b3) = network.GetWeights();

        Assert.Equal(16, w1.GetLength(0));
        Assert.Equal(32, w1.GetLength(1));
        Assert.Equal(32, b1.Length);
        Assert.Equal(32, w2.GetLength(0));
        Assert.Equal(Math.Max(32 / 2, 16), w2.GetLength(1));
        Assert.Equal(Math.Max(32 / 2, 16), b2.Length);
        Assert.Equal(Math.Max(32 / 2, 16), w3.GetLength(0));
        Assert.Equal(8, w3.GetLength(1));
        Assert.Equal(8, b3.Length);
    }

    [Fact]
    public void SetWeights_CorrectlyUpdatesNetwork()
    {
        var network = new MlpNetwork(inputSize: 16, hiddenSize: 32, outputSize: 8, seed: 42);
        var features = GenerateRandomFeatures(16);

        double[] outputBefore = network.Predict(features);

        var (w1, b1, w2, b2, w3, b3) = network.GetWeights();
        for (int i = 0; i < w1.GetLength(0); i++)
            for (int j = 0; j < w1.GetLength(1); j++)
                w1[i, j] *= 2.0;

        network.SetWeights(w1, b1, w2, b2, w3, b3);

        double[] outputAfter = network.Predict(features);

        bool allSame = outputBefore.Zip(outputAfter, (a, b) => Math.Abs(a - b) < 1e-10).All(x => x);
        Assert.False(allSame);
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MlpNetwork(null!));
    }

    private static double[] GenerateRandomFeatures(int size, int seed = 42)
    {
        var random = new Random(seed);
        var features = new double[size];
        for (int i = 0; i < size; i++)
        {
            features[i] = random.NextDouble();
        }
        return features;
    }
}