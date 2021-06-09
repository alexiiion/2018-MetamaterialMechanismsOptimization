using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ShearCell_Interaction.Helper;
using ShearCell_Interaction.Model;
using ShearCell_Interaction.Presenter;
using ShearCell_Interaction.Simulation;
using ShearCell_Interaction.View;

namespace ShearCell_Interaction.Optimisation
{
    public class AutoOptimization
    {
        private readonly ViewModel _viewModel;
        private readonly MetamaterialPresenter _presenter;

        private const int NUM_CURVES = 4;
        private const int GRID_SIDE_LENGTH = 5;

        public AutoOptimization(ViewModel viewModel, MetamaterialPresenter presenter)
        {
            _viewModel = viewModel;
            _presenter = presenter;
        }

        public void Start()
        {
            _viewModel.MaxNumScalesOptimization = 5;
            _viewModel.NumberPathSamples = 30;
            _viewModel.NumberIterationsGeneration = 200;
            _viewModel.BruteForceAnchors = true;

            for (int i = 0; i < NUM_CURVES; i++)
            {
                for (int j = 0; j < NUM_CURVES; j++)
                {
                    var logger = new CsvLogger("auto_curve_" + i + "_" + j + "_solution" + ".csv");

                    ResetModelForOptimizer(GRID_SIDE_LENGTH, GRID_SIDE_LENGTH);

                    _viewModel.Model.InputPath = GetCurveForState(i, true, _viewModel.Model.InputVertex.ToVector());
                    _viewModel.Model.OutputPath = GetCurveForState(i, false, _viewModel.Model.OutputVertex.ToVector());

                    _viewModel.InputPath = new PointCollection(_viewModel.Model.InputPath.Select(v => new Point(CoordinateConverter.ConvertGlobalToScreenCoordinates(new Vector(v.X, v.Y)).X, CoordinateConverter.ConvertGlobalToScreenCoordinates(new Vector(v.X, v.Y)).Y)));
                    _viewModel.OutputPath = new PointCollection(_viewModel.Model.OutputPath.Select(v => new Point(CoordinateConverter.ConvertGlobalToScreenCoordinates(new Vector(v.X, v.Y)).X, CoordinateConverter.ConvertGlobalToScreenCoordinates(new Vector(v.X, v.Y)).Y)));

                    _presenter.ExportCurrentConfiguration("auto_curve_" + i + "_" + j + "_input" + ".txt");

                    var optimizer = new HierarchicalOptimization(_viewModel.Model, _viewModel, _presenter);
                    optimizer.FilePrefix = "curve_" + i + "_" + j;
                    logger.AddOrChangePrefix("curve_" + i + "_" + j + "_", this);
                    optimizer.Logger = logger;

                    optimizer.Start();

                    _presenter.ExportCurrentConfiguration("auto_curve_" + i + "_" + j + "_solution.txt");

                    Console.WriteLine(@"Done for curve " + i + @"/" + j);

                    _presenter.Clear();
                    _presenter.Reset();
                }
            }
        }

        public void StartForFiles()
        {
            _viewModel.MaxNumScalesOptimization = 5;
            _viewModel.NumberPathSamples = 30;
            _viewModel.NumberIterationsGeneration = 200;
            _viewModel.BruteForceAnchors = true;

            var files = new List<string>
            {
                //@"01-curve_hook-03-4x4.txt",
                //@"02-curve_wave-01-4x4.txt",
                //@"03-doorlatch-01-4x4.txt",
                //@"04-backforth-01-4x4.txt",
                //@"05-rotation-01-4x4.txt",
                //@"06-corner-wave-01-4x4.txt",
                //@"07-flat-curve-01-4x4.txt",
                //@"08-scaling-01-4x4.txt",
                //@"09-curve-corner-01-4x4.txt",
                //@"10-oscillation-01-4x4.txt",
                //@"4x4_03_0111110111101011.txt",
                //@"4x4_07_0111110110101111.txt"
                //@"4x4_18_0010001011010010.txt",
                //@"4x4_10_0011001111001100.txt",
                //@"3x3_7_000111000.txt",
                //@"4_3_0005_0111110111011011_a6_i2_s1-out-set.txt",
                 //@"pickplace-09-4x4.txt",
                //@"walker-03-4x4.txt",
                //@"anchor-test-04.txt",
                //@"anchor-test-05.txt",
                //@"anchor-test-06.txt",
                //@"anchor-test-07.txt",
                //@"anchor-test-08.txt",
                //@"4x4_18_0010001011010010.txt",
                @"gripper-4x4-01.txt"
            };

            for (int i = 0; i < files.Count; i++)
            {
                var filename = files[i];
                string extension = System.IO.Path.GetExtension(filename);
                string filenameNoExt = extension != null ? filename.Substring(0, filename.Length - extension.Length) : filename;

                var logger = new CsvLogger("auto_solution_" + filenameNoExt + ".csv");

                ModelSerializer.ReadFromFile(filename, _viewModel.Model, _viewModel);

                var optimizer = new HierarchicalOptimization(_viewModel.Model, _viewModel, _presenter);
                //optimizer.SetupLogger();

                optimizer.FilePrefix = filenameNoExt;
                logger.AddOrChangePrefix(i + "_" + filename, this);
                optimizer.Logger = logger;

                optimizer.Start();

                _presenter.ExportCurrentConfiguration("auto_solution_" + filename);

                Console.WriteLine(@"Done for file " + i + @" / " + filename);

                _presenter.Clear();
                _presenter.Reset();
            }
        }

        private void ResetModelForOptimizer(int sizeX, int sizeY)
        {
            _viewModel.Model.Clear();

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    _viewModel.Model.AddCell(new ShearCell(), new Vector(x, y), new Size(1, 1), false);
                }
            }

            _viewModel.Model.UpdateConstraintsGraph();

            var inputVertex = _viewModel.Model.Vertices.Find(current =>
                MathHelper.IsEqualDouble(current.X, 0) && MathHelper.IsEqualDouble(current.Y, 0));

            if (inputVertex == null)
                throw new Exception("No cell at 0/0.");

            _viewModel.Model.InputVertex = inputVertex;

            var outputVertex = _viewModel.Model.Vertices.Find(current =>
                MathHelper.IsEqualDouble(current.X, sizeX) && MathHelper.IsEqualDouble(current.Y, sizeY));

            if (outputVertex == null)
                throw new Exception("No cell at 0/0.");

            _viewModel.Model.OutputVertex = outputVertex;
            //_viewModel.Model.OutputPath = CurveDrawingHelper.GetLine(100, 2.0, new Vector(sizeX - 1, sizeY - 1));

            //_viewModel.Model.SetAnchor(new Vector(4, 4), true);
            //_viewModel.Model.SetAnchor(new Vector(4, 5), true);
            //_viewModel.Model.SetAnchor(new Vector(5, 5), true);
            //_viewModel.Model.SetAnchor(new Vector(5, 4), true);
        }

        public List<Vector> GetCurveForState(int state, bool isInput, Vector start)
        {
            List<Vector> curve;
            double rotation = 0;
            double scaleX = 1.0, scaleY = 1.0;
            double commonScaleFactor = .5;

            switch (state)
            {
                case 0:
                    curve = CurveDrawingHelper.GetCubicPolynomial(100, .1, new Vector(0, 0));
                    rotation = isInput ? 215 : 45;
                    scaleX = 1.0 ;
                    scaleY = .5;
                    break;
                case 1:
                    curve = CurveDrawingHelper.GetFoliumDescartes(100, .6, new Vector(0, 0));
                    rotation = isInput ? 0 : 35;
                    scaleX = 1.0;
                    scaleY = isInput ? 1.0 : -1.0;
                    break;
                case 2:
                    curve = CurveDrawingHelper.GetTransformedBicorn(100, 1.0, new Vector(0, 0));
                    rotation = isInput ? 0 : 90;
                    scaleX = 1.0;
                    scaleY = 1.0;
                    break;
                case 3:
                    curve = CurveDrawingHelper.GetBowCurve(100, 1.0, new Vector(0, 0));
                    rotation = isInput ? 0 : 60;
                    scaleX = 1.0;
                    scaleY = isInput ? 1.0 : -1.0;
                    break;
                default:
                    throw new Exception("no curve for state " + state);
            }

            var scaledOutputPath = PolylineHelper.Scale(curve, scaleX * commonScaleFactor, scaleY * commonScaleFactor);
            var translatedOutputPath = PolylineHelper.Translate(scaledOutputPath, start.X, start.Y);
            var rotatedOutputPath = PolylineHelper.Rotate(translatedOutputPath, rotation, start.X, start.Y);

            return rotatedOutputPath;
        }
    }
}