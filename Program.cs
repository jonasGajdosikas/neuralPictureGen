using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace neuralPictureGen
{
    class Program
    {
        static void Main()
        {
            for(int i = 0; i < 100; i++)
            {
                Console.WriteLine("Working on image {0}/100", i+1);
                DateTime dateTime = DateTime.Now;
                string timeNow = dateTime.ToString("yy-MM-dd HHmm");
                //Console.WriteLine("Time now is " + timeNow);
                int width = 1080, height = 2340;
                //int width = 200, height = 200;
                //Console.WriteLine("setting up net");
                // (NStart, NEnd, NLayers, NinLayer
                NeuralNet net = new(3, 3, 8, 12, width, height);
                //Console.WriteLine("set up the neural net");
                Bitmap bitmap = new(width, height);
                //Console.WriteLine("Started evaluating the pixels");
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        bitmap.SetPixel(x, y, net.Evaluate(x, y, i));
                    }
                }
                bitmap.Save("images\\image " + timeNow + ".png");
            }
            Console.WriteLine("Done");
            /**/
            //Console.ReadKey();
        }
    }
    class NeuralNet
    {
        public static double WeightLimit = 4.0;
        public List<StartNode> StartNodes;
        public List<List<Node>> HiddenNodes;
        public List<EndNode> EndNodes;
        public int width, height;
        public int XOffset, YOffset, ZOffset;
        public int Nstart, NEnd, NLayers, NodesInLayer;
        public NeuralNet(int _NStart, int _NEnd, int _NLayers, int _NNodesInLayer, int _width, int _height)
        {
            Nstart = _NStart;
            NEnd = _NEnd;
            NLayers = _NLayers;
            NodesInLayer = _NNodesInLayer;
            width = _width;
            height = _height;
            Random random = new();
            XOffset = random.Next(-5000, 5000);
            YOffset = random.Next(-5000, 5000);
            ZOffset = random.Next(-5000, 5000);
            StartNodes = new List<StartNode>();
            while (StartNodes.Count < Nstart) StartNodes.Add(new StartNode());
            HiddenNodes = new();
            List<Node> nodes = new();
            while(nodes.Count < NodesInLayer)
            {
                List<double> weights = new();
                for (int i = 0; i < Nstart; i++)
                {
                    weights.Add((random.NextDouble()-0.5) * 2 * WeightLimit);
                    //Console.WriteLine(weights[i]);
                }
                nodes.Add(new Node(Nstart, weights.ToArray()));
            }
            HiddenNodes.Add(nodes);
            //while (HiddenNodes.Count < NLayers)
            for (int i = 1; i < NLayers; i++)
            {
                nodes = new List<Node>();
                while (nodes.Count < NodesInLayer - i)
                {
                    List<double> weights = new();
                    for (int j = 0; j < NodesInLayer + 1 - i; j++)
                    {
                        weights.Add((random.NextDouble() - 0.5) * 2 * WeightLimit);
                    }
                    nodes.Add(new Node(NodesInLayer + 1 - i, weights.ToArray()));
                }
                HiddenNodes.Add(nodes);
            }
            EndNodes = new List<EndNode>();
            while (EndNodes.Count < NEnd)
            {
                List<double> weights = new();
                for (int i = 0; i < NodesInLayer; i++)
                {
                    weights.Add((random.NextDouble() - 0.5) * 2 * WeightLimit);
                }
                EndNodes.Add(new EndNode(NodesInLayer - NLayers, weights.ToArray()));
            }
        }
        public static double[] Outputs(List<Node> nodes)
        {
            List<double> _outputs = new();
            foreach (Node node in nodes) _outputs.Add(node.Output);
            return _outputs.ToArray();
        }
        public static double[] Outputs(List<StartNode> nodes)
        {
            List<double> _outputs = new();
            foreach (StartNode node in nodes) _outputs.Add(node.Output);
            return _outputs.ToArray();
        }
        public Color Evaluate(int Xpos, int Ypos, int Zpos)
        {
            //set values and outputs for first layer (takes in x and y)
            double ZoomFactor = 0.1;
            StartNodes[0].Value = (((double)Xpos) / (double)width) - 0.5;
            StartNodes[1].Value = (((double)Ypos) / (double)height) - 0.5;
            StartNodes[2].Value = Perlin.OctavePerlin((ZoomFactor * Xpos + (double)XOffset) / width, (ZoomFactor * Ypos + (double)YOffset) / height, (ZoomFactor * Zpos + (double)ZOffset)/ 1590, 3, 0.7);
            //Console.WriteLine("LS: {0}, {1}", StartNodes[0].Value, StartNodes[1].Value);
            /**
            double ScalingFactor = Math.Sqrt(Math.Sqrt(StartNodes[0].Value * StartNodes[0].Value + StartNodes[1].Value * StartNodes[1].Value));
            if (ScalingFactor == 0) ScalingFactor = 1;
            /**/
            //Console.WriteLine(ScalingFactor);
            StartNodes[0].Output = StartNodes[0].Value * ZoomFactor * ZoomFactor;
            StartNodes[1].Output = StartNodes[1].Value * ZoomFactor * ZoomFactor;
            StartNodes[2].Output = StartNodes[2].Value * ZoomFactor * ZoomFactor;
            //Console.WriteLine("LS: {0}, {1}", StartNodes[0].Output, StartNodes[1].Output);
            //saves the outputs as an array, passes them down to next layer

            double[] values = Outputs(StartNodes);
            for (int i = 0; i < NLayers; i++)
            {
                //Console.Write("L{0}:",i+1);
                //foreach (Node node in HiddenNodes[i]) node.Evaluate(values);
                for (int j = 0; j < NodesInLayer - i; j++)
                {
                    HiddenNodes[i][j].Evaluate(values);
                    //Console.Write(" {0},", HiddenNodes[i][j].Output);
                }
                //Console.WriteLine();
                values = Outputs(HiddenNodes[i]);
            }

            //foreach (EndNode node in EndNodes) node.Evaluate(values);
            //Console.Write("LE:");
            for (int i = 0; i < NEnd; i++)
            {
                EndNodes[i].Evaluate(values);
                //Console.Write(" {0},", EndNodes[i].Output);
            }

            int r = (int)EndNodes[0].Output,
                g = (int)EndNodes[1].Output,
                b = (int)EndNodes[2].Output;
            if (r + 2 * g + b > 256 + 128)
            {
                r = r * 255 / (r + 2 * g + b);
                g = g * 127 / (r + 2 * g + b);
                b = b * 255 / (r + 2 * g + b);
            }
            //Console.WriteLine("({0},{1}):{2},{3},{4}", Xpos, Ypos, r, g, b);
            return Color.FromArgb(r, g, b);
        }
    }
    class Node
    {
        public int NoOfParents;
        public double[] Weights;
        public double Value;
        public double Output;
        public Node(int _Parents, double[] _weights)
        {
            NoOfParents = _Parents;
            Weights = _weights;
        }
        public void Evaluate(double[] _inputs)
        {
            Value = 0;
            for (int i = 0; i < NoOfParents; i++) Value += Weights[i] * _inputs[i];
            Output = Math.Tanh(Value);
        }
    }
    class StartNode : Node
    {
        public StartNode() : base(0, Array.Empty<double>())
        {
            
        }
    }
    class EndNode : Node
    {
        public EndNode(int _NoParents, double[] _weights) : base(_NoParents, _weights)
        {

        }
        new public void Evaluate(double[] _inputs)
        {
            Value = 0;
            for (int i = 0; i < NoOfParents; i++) Value += Weights[i] * _inputs[i];
            Output = 255 / (1 + Math.Exp(Value));
        }
    }
    public class Perlin
    {

        public static double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            for (int i = 0; i < octaves; i++)
            {
                total += PerlinValue(x * frequency, y * frequency, z * frequency) * amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total;
        }

        private static readonly int[] permutation = { 151,160,137,91,90,15,					// Hash lookup table as defined by Ken Perlin.  This is a randomly
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,	    // arranged array of all numbers from 0-255 inclusive.
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

        private static readonly int[] p;                                                    // Doubled permutation to avoid overflow

        static Perlin()
        {
            p = new int[512];
            for (int x = 0; x < 512; x++)
            {
                p[x] = permutation[x % 256];
            }
        }

        public static double PerlinValue(double x, double y, double z)
        {

            int xi = (int)x & 255;                              // Calculate the "unit cube" that the point asked will be located in
            int yi = (int)y & 255;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            int zi = (int)z & 255;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            double xf = x - (int)x;                             // We also fade the location to smooth the result.
            double yf = y - (int)y;
            double zf = z - (int)z;
            double u = Fade(xf);
            double v = Fade(yf);
            double w = Fade(zf);

            int a = p[xi] + yi;                             // This here is Perlin's hash function.  We take our x value (remember,
            int aa = p[a] + zi;                             // between 0 and 255) and get a random value (from our p[] array above) between
            int ab = p[a + 1] + zi;                         // 0 and 255.  We then add y to it and plug that into p[], and add z to that.
            int b = p[xi + 1] + yi;                         // Then, we get another random value by adding 1 to that and putting it into p[]
            int ba = p[b] + zi;                             // and add z to it.  We do the whole thing over again starting with x+1.  Later
            int bb = p[b + 1] + zi;                         // we plug aa, ab, ba, and bb back into p[] along with their +1's to get another set.
                                                            // in the end we have 8 values between 0 and 255 - one for each vertex on the unit cube.
                                                            // These are all interpolated together using u, v, and w below.

            double x1, x2, y1, y2;
            x1 = Lerp(Grad(p[aa], xf, yf, zf),              // This is where the "magic" happens.  We calculate a new set of p[] values and use that to get
                        Grad(p[ba], xf - 1, yf, zf),        // our final gradient values.  Then, we interpolate between those gradients with the u value to get
                        u);                                 // 4 x-values.  Next, we interpolate between the 4 x-values with v to get 2 y-values.  Finally,
            x2 = Lerp(Grad(p[ab], xf, yf - 1, zf),          // we interpolate between the y-values to get a z-value.
                        Grad(p[bb], xf - 1, yf - 1, zf),
                        u);                                 // When calculating the p[] values, remember that above, p[a+1] expands to p[xi]+yi+1 -- so you are
            y1 = Lerp(x1, x2, v);                           // essentially adding 1 to yi.  Likewise, p[ab+1] expands to p[p[xi]+yi+1]+zi+1] -- so you are adding
                                                            // to zi.  The other 3 parameters are your possible return values (see grad()), which are actually
            x1 = Lerp(Grad(p[aa + 1], xf, yf, zf - 1),      // the vectors from the edges of the unit cube to the point in the unit cube itself.
                        Grad(p[ba + 1], xf - 1, yf, zf - 1),
                        u);
            x2 = Lerp(Grad(p[ab + 1], xf, yf - 1, zf - 1),
                          Grad(p[bb + 1], xf - 1, yf - 1, zf - 1),
                          u);
            y2 = Lerp(x1, x2, v);

            return (Lerp(y1, y2, w) + 1) / 2;               // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        }

        public static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;                                  // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            double u = h < 8 /* 0b1000 */ ? x : y;              // If the most signifigant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.

            double v;                                           // In Ken Perlin's original implementation this was another conditional operator (?:).  I
                                                                // expanded it for readability.

            if (h < 4 /* 0b0100 */)                             // If the first and second signifigant bits are 0 set v = y
                v = y;
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)// If the first and second signifigant bits are 1 set v = x
                v = x;
            else                                                // If the first and second signifigant bits are not equal (0/1, 1/0) set v = z
                v = z;

            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
        }

        public static double Fade(double t)
        {
            // Fade function as defined by Ken Perlin.  This eases coordinate values
            // so that they will "ease" towards integral values.  This ends up smoothing
            // the final output.
            return t * t * t * (t * (t * 6 - 15) + 10);         // 6t^5 - 15t^4 + 10t^3
        }

        public static double Lerp(double a, double b, double x)
        {
            return a + x * (b - a);
        }
    }
}
