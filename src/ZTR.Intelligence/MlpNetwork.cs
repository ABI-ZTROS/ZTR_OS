using ZTR.Models;

namespace ZTR.Intelligence;

/// <summary>
/// A lightweight fully-connected Multi-Layer Perceptron neural network.
/// Uses ReLU activation for hidden layers and Sigmoid for the output layer
/// to produce normalized action values in [0, 1].
/// </summary>
public class MlpNetwork
{
    private readonly int _inputSize;
    private readonly int _hiddenSize;
    private readonly int _outputSize;
    private readonly int _hiddenSize2;

    private double[,] _weights1 = new double[0, 0];
    private double[] _bias1 = System.Array.Empty<double>();
    private double[,] _weights2 = new double[0, 0];
    private double[] _bias2 = System.Array.Empty<double>();
    private double[,] _weights3 = new double[0, 0];
    private double[] _bias3 = System.Array.Empty<double>();

    private readonly Random _random;

    /// <summary>
    /// Gets the input layer size.
    /// </summary>
    public int InputSize => _inputSize;

    /// <summary>
    /// Gets the hidden layer 1 size.
    /// </summary>
    public int HiddenSize => _hiddenSize;

    /// <summary>
    /// Gets the hidden layer 2 size.
    /// </summary>
    public int HiddenSize2 => _hiddenSize2;

    /// <summary>
    /// Gets the output layer size.
    /// </summary>
    public int OutputSize => _outputSize;

    /// <summary>
    /// Creates a new instance of the <see cref="MlpNetwork"/> class.
    /// </summary>
    /// <param name="config">MLP configuration parameters.</param>
    /// <param name="seed">Random seed for reproducible initialization (0 for auto).</param>
    public MlpNetwork(MlpConfig config, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(config);

        _inputSize = config.InputSize;
        _hiddenSize = config.HiddenLayerSize;
        _outputSize = config.OutputSize;
        _hiddenSize2 = Math.Max(_hiddenSize / 2, 16);

        _random = seed == 0 ? new Random() : new Random(seed);

        InitializeWeights();
    }

    /// <summary>
    /// Creates a new instance with specific layer sizes.
    /// </summary>
    /// <param name="inputSize">Number of input features.</param>
    /// <param name="hiddenSize">Number of neurons in the first hidden layer.</param>
    /// <param name="outputSize">Number of output actions.</param>
    /// <param name="seed">Random seed for reproducible initialization.</param>
    public MlpNetwork(int inputSize, int hiddenSize, int outputSize, int seed = 0)
    {
        _inputSize = inputSize;
        _hiddenSize = hiddenSize;
        _outputSize = outputSize;
        _hiddenSize2 = Math.Max(hiddenSize / 2, 16);

        _random = seed == 0 ? new Random() : new Random(seed);

        InitializeWeights();
    }

    /// <summary>
    /// Initializes weights using He initialization for ReLU networks.
    /// </summary>
    private void InitializeWeights()
    {
        _weights1 = HeInitialize(_inputSize, _hiddenSize);
        _bias1 = new double[_hiddenSize];

        _weights2 = HeInitialize(_hiddenSize, _hiddenSize2);
        _bias2 = new double[_hiddenSize2];

        _weights3 = HeInitialize(_hiddenSize2, _outputSize);
        _bias3 = new double[_outputSize];
    }

    /// <summary>
    /// Performs He initialization for a layer.
    /// </summary>
    private double[,] HeInitialize(int fanIn, int fanOut)
    {
        var weights = new double[fanIn, fanOut];
        double std = Math.Sqrt(2.0 / fanIn);

        for (int i = 0; i < fanIn; i++)
        {
            for (int j = 0; j < fanOut; j++)
            {
                weights[i, j] = (_random.NextDouble() * 2.0 - 1.0) * std;
            }
        }

        return weights;
    }

    /// <summary>
    /// Performs forward propagation through the network to predict action values.
    /// </summary>
    /// <param name="features">Input feature vector matching <see cref="InputSize"/>.</param>
    /// <returns>Normalized action vector in [0, 1] range matching <see cref="OutputSize"/>.</returns>
    public double[] Predict(double[] features)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (features.Length != _inputSize)
            throw new ArgumentException($"Expected {_inputSize} features but got {features.Length}.");

        double[] h1 = ComputeLayer(features, _weights1, _bias1, _hiddenSize, ReLU);
        double[] h2 = ComputeLayer(h1, _weights2, _bias2, _hiddenSize2, ReLU);
        double[] output = ComputeLayer(h2, _weights3, _bias3, _outputSize, Sigmoid);

        return output;
    }

    /// <summary>
    /// Computes a single neural layer: output = activation(input * weights + bias).
    /// </summary>
    private static double[] ComputeLayer(double[] input, double[,] weights, double[] bias, int outputSize, Func<double, double> activation)
    {
        var result = new double[outputSize];
        for (int j = 0; j < outputSize; j++)
        {
            double sum = bias[j];
            for (int i = 0; i < input.Length; i++)
            {
                double w = weights[i, j];
                double x = input[i];
                if (double.IsNaN(w) || double.IsInfinity(w)) w = 0.0;
                if (double.IsNaN(x) || double.IsInfinity(x)) x = 0.0;
                sum += x * w;
            }
            result[j] = activation(sum);
        }
        return result;
    }

    /// <summary>
    /// Rectified Linear Unit activation.
    /// </summary>
    private static double ReLU(double x) => Math.Max(0.0, x);

    /// <summary>
    /// Sigmoid activation for output normalization.
    /// </summary>
    private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

    /// <summary>
    /// Gets a shallow copy of the network weights for persistence.
    /// </summary>
    /// <returns>A tuple containing all weight and bias arrays.</returns>
    public (double[,] w1, double[] b1, double[,] w2, double[] b2, double[,] w3, double[] b3) GetWeights()
    {
        return (_weights1, _bias1, _weights2, _bias2, _weights3, _bias3);
    }

    /// <summary>
    /// Sets the network weights from persisted values.
    /// </summary>
    /// <param name="w1">Layer 1 weights.</param>
    /// <param name="b1">Layer 1 biases.</param>
    /// <param name="w2">Layer 2 weights.</param>
    /// <param name="b2">Layer 2 biases.</param>
    /// <param name="w3">Layer 3 weights.</param>
    /// <param name="b3">Layer 3 biases.</param>
    public void SetWeights(double[,] w1, double[] b1, double[,] w2, double[] b2, double[,] w3, double[] b3)
    {
        _weights1 = w1;
        _bias1 = b1;
        _weights2 = w2;
        _bias2 = b2;
        _weights3 = w3;
        _bias3 = b3;
    }

    /// <summary>
    /// Gets direct access to weight arrays for the online learner.
    /// </summary>
    internal double[,] W1 => _weights1;
    internal double[] B1 => _bias1;
    internal double[,] W2 => _weights2;
    internal double[] B2 => _bias2;
    internal double[,] W3 => _weights3;
    internal double[] B3 => _bias3;
}