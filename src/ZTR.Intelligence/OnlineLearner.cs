using System.IO;
using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// Provides online Stochastic Gradient Descent training for the MLP network.
/// Supports adaptive learning rate decay, mini-batch updates, and weight persistence.
/// </summary>
public class OnlineLearner
{
    private readonly MlpNetwork _network;
    private readonly double _initialLearningRate;
    private readonly double _decayRate;
    private readonly double _gradientClipThreshold;
    private int _updateCount;
    private double _currentLearningRate;

    /// <summary>
    /// Gets the current learning rate after decay scheduling.
    /// </summary>
    public double CurrentLearningRate => _currentLearningRate;

    /// <summary>
    /// Gets the total number of weight updates performed.
    /// </summary>
    public int UpdateCount => _updateCount;

    /// <summary>
    /// Creates a new instance of the <see cref="OnlineLearner"/> class.
    /// </summary>
    /// <param name="network">The MLP network to train.</param>
    /// <param name="initialLearningRate">Initial learning rate (default 0.01).</param>
    /// <param name="decayRate">Exponential decay rate per update (default 0.001).</param>
    /// <param name="gradientClipThreshold">Gradient clipping threshold to prevent weight explosion (default 1.0).</param>
    public OnlineLearner(MlpNetwork network, double initialLearningRate = 0.01, double decayRate = 0.001, double gradientClipThreshold = 1.0)
    {
        ArgumentNullException.ThrowIfNull(network);
        _network = network;
        _initialLearningRate = initialLearningRate;
        _decayRate = decayRate;
        _gradientClipThreshold = gradientClipThreshold;
        _currentLearningRate = initialLearningRate;
    }

    /// <summary>
    /// Trains the network on a single sample using online SGD with backpropagation.
    /// </summary>
    /// <param name="sample">The training sample containing features and target.</param>
    /// <returns>The squared error loss for this sample before the update.</returns>
    public double Train(MlpTrainingSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (sample.Features.Length != _network.InputSize)
            throw new ArgumentException($"Feature size mismatch: expected {_network.InputSize}, got {sample.Features.Length}.");
        if (sample.Target.Length != _network.OutputSize)
            throw new ArgumentException($"Target size mismatch: expected {_network.OutputSize}, got {sample.Target.Length}.");

        var activations = ForwardPass(sample.Features);
        double loss = ComputeSquaredError(activations.Output, sample.Target);

        BackwardPass(sample.Features, sample.Target, activations);

        _updateCount++;
        _currentLearningRate = _initialLearningRate / (1.0 + _decayRate * _updateCount);

        return loss;
    }

    /// <summary>
    /// Trains the network on a mini-batch of samples using averaged gradients.
    /// </summary>
    /// <param name="batch">Array of training samples.</param>
    /// <returns>The average squared error loss across the batch before the update.</returns>
    public double TrainBatch(MlpTrainingSample[] batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Length == 0)
            return 0.0;

        double totalLoss = 0.0;

        var gradW1 = new double[_network.InputSize, _network.HiddenSize];
        var gradB1 = new double[_network.HiddenSize];
        var gradW2 = new double[_network.HiddenSize, _network.HiddenSize2];
        var gradB2 = new double[_network.HiddenSize2];
        var gradW3 = new double[_network.HiddenSize2, _network.OutputSize];
        var gradB3 = new double[_network.OutputSize];

        foreach (var sample in batch)
        {
            var activations = ForwardPass(sample.Features);
            totalLoss += ComputeSquaredError(activations.Output, sample.Target);

            AccumulateGradients(sample.Features, sample.Target, activations,
                gradW1, gradB1, gradW2, gradB2, gradW3, gradB3);
        }

        int n = batch.Length;
        double lr = _currentLearningRate;

        ApplyGradients(gradW1, gradB1, gradW2, gradB2, gradW3, gradB3, n, lr);

        _updateCount++;
        _currentLearningRate = _initialLearningRate / (1.0 + _decayRate * _updateCount);

        return totalLoss / n;
    }

    /// <summary>
    /// Performs a forward pass and captures intermediate activations for backpropagation.
    /// </summary>
    private (double[] H1, double[] H2, double[] Output) ForwardPass(double[] features)
    {
        double[] h1 = new double[_network.HiddenSize];
        double[] h2 = new double[_network.HiddenSize2];
        double[] output = new double[_network.OutputSize];

        var w1 = _network.W1;
        var b1 = _network.B1;
        var w2 = _network.W2;
        var b2 = _network.B2;
        var w3 = _network.W3;
        var b3 = _network.B3;

        for (int j = 0; j < _network.HiddenSize; j++)
        {
            double sum = b1[j];
            for (int i = 0; i < _network.InputSize; i++)
                sum += features[i] * w1[i, j];
            h1[j] = Math.Min(ReLU(sum), 100.0);
        }

        for (int j = 0; j < _network.HiddenSize2; j++)
        {
            double sum = b2[j];
            for (int i = 0; i < _network.HiddenSize; i++)
                sum += h1[i] * w2[i, j];
            h2[j] = Math.Min(ReLU(sum), 100.0);
        }

        for (int j = 0; j < _network.OutputSize; j++)
        {
            double sum = b3[j];
            for (int i = 0; i < _network.HiddenSize2; i++)
                sum += h2[i] * w3[i, j];
            output[j] = Sigmoid(sum);
        }

        return (h1, h2, output);
    }

    /// <summary>
    /// Performs backward pass and updates weights for a single sample (online SGD).
    /// </summary>
    private void BackwardPass(double[] features, double[] target,
        (double[] H1, double[] H2, double[] Output) activations)
    {
        var w3 = _network.W3;
        var b3 = _network.B3;
        var w2 = _network.W2;
        var b2 = _network.B2;
        var w1 = _network.W1;
        var b1 = _network.B1;

        double lr = _currentLearningRate;

        var delta3 = new double[_network.OutputSize];
        for (int j = 0; j < _network.OutputSize; j++)
        {
            double error = activations.Output[j] - target[j];
            delta3[j] = ClipGradient(error * activations.Output[j] * (1.0 - activations.Output[j]));
        }

        for (int j = 0; j < _network.HiddenSize2; j++)
        {
            for (int k = 0; k < _network.OutputSize; k++)
                w3[j, k] -= lr * delta3[k] * activations.H2[j];
        }
        for (int k = 0; k < _network.OutputSize; k++)
            b3[k] -= lr * delta3[k];

        var delta2 = new double[_network.HiddenSize2];
        for (int j = 0; j < _network.HiddenSize2; j++)
        {
            double sum = 0.0;
            for (int k = 0; k < _network.OutputSize; k++)
                sum += delta3[k] * w3[j, k];
            delta2[j] = ClipGradient(sum * (activations.H2[j] > 0 ? 1.0 : 0.0));
        }

        for (int i = 0; i < _network.HiddenSize; i++)
        {
            for (int j = 0; j < _network.HiddenSize2; j++)
                w2[i, j] -= lr * delta2[j] * activations.H1[i];
        }
        for (int j = 0; j < _network.HiddenSize2; j++)
            b2[j] -= lr * delta2[j];

        var delta1 = new double[_network.HiddenSize];
        for (int i = 0; i < _network.HiddenSize; i++)
        {
            double sum = 0.0;
            for (int j = 0; j < _network.HiddenSize2; j++)
                sum += delta2[j] * w2[i, j];
            delta1[i] = ClipGradient(sum * (activations.H1[i] > 0 ? 1.0 : 0.0));
        }

        for (int i = 0; i < _network.InputSize; i++)
        {
            for (int j = 0; j < _network.HiddenSize; j++)
                w1[i, j] -= lr * delta1[j] * features[i];
        }
        for (int j = 0; j < _network.HiddenSize; j++)
            b1[j] -= lr * delta1[j];

        ClipWeights(w1, b1);
        ClipWeights(w2, b2);
        ClipWeights(w3, b3);
    }

    /// <summary>
    /// Accumulates gradients for mini-batch training.
    /// </summary>
    private void AccumulateGradients(double[] features, double[] target,
        (double[] H1, double[] H2, double[] Output) activations,
        double[,] gradW1, double[] gradB1, double[,] gradW2, double[] gradB2,
        double[,] gradW3, double[] gradB3)
    {
        var w3 = _network.W3;
        var w2 = _network.W2;

        var delta3 = new double[_network.OutputSize];
        for (int j = 0; j < _network.OutputSize; j++)
        {
            double error = activations.Output[j] - target[j];
            delta3[j] = error * activations.Output[j] * (1.0 - activations.Output[j]);
        }

        for (int j = 0; j < _network.HiddenSize2; j++)
            for (int k = 0; k < _network.OutputSize; k++)
                gradW3[j, k] += delta3[k] * activations.H2[j];
        for (int k = 0; k < _network.OutputSize; k++)
            gradB3[k] += delta3[k];

        var delta2 = new double[_network.HiddenSize2];
        for (int j = 0; j < _network.HiddenSize2; j++)
        {
            double sum = 0.0;
            for (int k = 0; k < _network.OutputSize; k++)
                sum += delta3[k] * w3[j, k];
            delta2[j] = sum * (activations.H2[j] > 0 ? 1.0 : 0.0);
        }

        for (int i = 0; i < _network.HiddenSize; i++)
            for (int j = 0; j < _network.HiddenSize2; j++)
                gradW2[i, j] += delta2[j] * activations.H1[i];
        for (int j = 0; j < _network.HiddenSize2; j++)
            gradB2[j] += delta2[j];

        var delta1 = new double[_network.HiddenSize];
        for (int i = 0; i < _network.HiddenSize; i++)
        {
            double sum = 0.0;
            for (int j = 0; j < _network.HiddenSize2; j++)
                sum += delta2[j] * w2[i, j];
            delta1[i] = sum * (activations.H1[i] > 0 ? 1.0 : 0.0);
        }

        for (int i = 0; i < _network.InputSize; i++)
            for (int j = 0; j < _network.HiddenSize; j++)
                gradW1[i, j] += delta1[j] * features[i];
        for (int j = 0; j < _network.HiddenSize; j++)
            gradB1[j] += delta1[j];
    }

    /// <summary>
    /// Applies accumulated averaged gradients to the network weights.
    /// </summary>
    private void ApplyGradients(double[,] gradW1, double[] gradB1,
        double[,] gradW2, double[] gradB2, double[,] gradW3, double[] gradB3,
        int batchSize, double lr)
    {
        var w1 = _network.W1;
        var b1 = _network.B1;
        var w2 = _network.W2;
        var b2 = _network.B2;
        var w3 = _network.W3;
        var b3 = _network.B3;

        for (int i = 0; i < _network.InputSize; i++)
            for (int j = 0; j < _network.HiddenSize; j++)
                w1[i, j] -= lr * gradW1[i, j] / batchSize;
        for (int j = 0; j < _network.HiddenSize; j++)
            b1[j] -= lr * gradB1[j] / batchSize;

        for (int i = 0; i < _network.HiddenSize; i++)
            for (int j = 0; j < _network.HiddenSize2; j++)
                w2[i, j] -= lr * gradW2[i, j] / batchSize;
        for (int j = 0; j < _network.HiddenSize2; j++)
            b2[j] -= lr * gradB2[j] / batchSize;

        for (int i = 0; i < _network.HiddenSize2; i++)
            for (int j = 0; j < _network.OutputSize; j++)
                w3[i, j] -= lr * gradW3[i, j] / batchSize;
        for (int j = 0; j < _network.OutputSize; j++)
            b3[j] -= lr * gradB3[j] / batchSize;
    }

    /// <summary>
    /// Computes the mean squared error between predicted and target outputs.
    /// </summary>
    private static double ComputeSquaredError(double[] predicted, double[] target)
    {
        double sum = 0.0;
        for (int i = 0; i < predicted.Length; i++)
        {
            double diff = predicted[i] - target[i];
            sum += diff * diff;
        }
        return sum / predicted.Length;
    }

    /// <summary>
    /// Saves the current network weights to a file for persistence.
    /// </summary>
    /// <param name="filePath">Path to the file where weights will be saved.</param>
    public void SaveWeights(string filePath)
    {
        var (w1, b1, w2, b2, w3, b3) = _network.GetWeights();

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(_network.InputSize);
        writer.Write(_network.HiddenSize);
        writer.Write(_network.HiddenSize2);
        writer.Write(_network.OutputSize);
        writer.Write(_updateCount);
        writer.Write(_currentLearningRate);

        WriteMatrix(writer, w1);
        WriteVector(writer, b1);
        WriteMatrix(writer, w2);
        WriteVector(writer, b2);
        WriteMatrix(writer, w3);
        WriteVector(writer, b3);
    }

    /// <summary>
    /// Loads network weights from a file.
    /// </summary>
    /// <param name="filePath">Path to the weights file.</param>
    /// <returns>True if weights were loaded successfully; otherwise false.</returns>
    public bool LoadWeights(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            int inputSize = reader.ReadInt32();
            int hiddenSize = reader.ReadInt32();
            int hiddenSize2 = reader.ReadInt32();
            int outputSize = reader.ReadInt32();
            _updateCount = reader.ReadInt32();
            _currentLearningRate = reader.ReadDouble();

            var w1 = ReadMatrix(reader, inputSize, hiddenSize);
            var b1 = ReadVector(reader, hiddenSize);
            var w2 = ReadMatrix(reader, hiddenSize, hiddenSize2);
            var b2 = ReadVector(reader, hiddenSize2);
            var w3 = ReadMatrix(reader, hiddenSize2, outputSize);
            var b3 = ReadVector(reader, outputSize);

            _network.SetWeights(w1, b1, w2, b2, w3, b3);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the learning rate to its initial value.
    /// </summary>
    public void ResetLearningRate()
    {
        _currentLearningRate = _initialLearningRate;
        _updateCount = 0;
    }

    private static void WriteMatrix(BinaryWriter writer, double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                writer.Write(matrix[i, j]);
    }

    private static void WriteVector(BinaryWriter writer, double[] vector)
    {
        foreach (var val in vector)
            writer.Write(val);
    }

    private static double[,] ReadMatrix(BinaryReader reader, int rows, int cols)
    {
        var matrix = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = reader.ReadDouble();
        return matrix;
    }

    private static double[] ReadVector(BinaryReader reader, int length)
    {
        var vector = new double[length];
        for (int i = 0; i < length; i++)
            vector[i] = reader.ReadDouble();
        return vector;
    }

    /// <summary>
    /// Clamps a gradient value to the configured threshold to prevent weight explosion.
    /// </summary>
    private double ClipGradient(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0.0;
        return Math.Clamp(value, -_gradientClipThreshold, _gradientClipThreshold);
    }

    /// <summary>
    /// Clamps all weight values in a layer to prevent overflow.
    /// </summary>
    private static void ClipWeights(double[,] weights, double[] biases)
    {
        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                if (double.IsNaN(weights[i, j]) || double.IsInfinity(weights[i, j]))
                    weights[i, j] = 0.0;
                else
                    weights[i, j] = Math.Clamp(weights[i, j], -100.0, 100.0);
            }
        }

        for (int i = 0; i < biases.Length; i++)
        {
            if (double.IsNaN(biases[i]) || double.IsInfinity(biases[i]))
                biases[i] = 0.0;
            else
                biases[i] = Math.Clamp(biases[i], -100.0, 100.0);
        }
    }

    private static double ReLU(double x) => Math.Max(0.0, x);
    private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));
}