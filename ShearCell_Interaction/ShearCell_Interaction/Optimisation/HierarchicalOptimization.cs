using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ShearCell_Interaction.Helper;
using ShearCell_Interaction.Model;
using ShearCell_Interaction.Presenter;
using ShearCell_Interaction.Simulation;
using ShearCell_Interaction.View;

namespace ShearCell_Interaction.Optimisation
{
    public class HierarchicalOptimization
    {
        private MetamaterialModel _model;
        private readonly ViewModel _viewModel;
        private readonly MetamaterialPresenter _presenter;

        private Random _random;

        private const int NUM_MAX_OPTI_RESTARTS = 10;
        private const int NUM_PARALLEL_OPTI_RUNS = 1;

        public HierarchicalOptimization(MetamaterialModel model, ViewModel viewModel, MetamaterialPresenter presenter)
        {
            _model = model;
            _viewModel = viewModel;
            _presenter = presenter;
            _random = new Random();

            FilePrefix = string.Empty;
            _random = new Random();
        }

        public string FilePrefix;

        public CsvLogger Logger { get; set; }

        public void SetupLogger()
        {
            var filenameDate = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

            Logger = new CsvLogger("log_" + filenameDate + ".csv");

            Logger.WriteHeader("DateTime", "Prefix", "Iteration",
                                "Error", "InputError", "OutputError", "MinError",
                                "Accepted", "ForceAccept", "SimTemp",
                                "DoF",
                                "InputPathLengthTarget", "OutputPathLengthTarget",
                                "InputPathLengthCurrent", "OutputPathLengthCurrent",
                                "InputInflectionPointsTarget", "OutputInflectionPointsTarget",
                                "InputPathLengthCurrent", "OutputPathLengthCurrent",
                                "BitCode");
        }

        public void Start()
        {
            var startTime = DateTime.Now;

            var resultDidImproveAfterScale = true;
            var currentNumOptimizationScales = 0;

            List<Vector> originalInputPath = new List<Vector>(_model.InputPath);
            List<Vector> originalOutputPath = new List<Vector>(_model.OutputPath);

            SimulationModel bestModel = null;

            while (resultDidImproveAfterScale && currentNumOptimizationScales < _viewModel.MaxNumScalesOptimization)
            {
                Console.WriteLine(@"Run for scale " + currentNumOptimizationScales);

                if (currentNumOptimizationScales == 0 && _viewModel.BruteForceAnchors)
                {
                    //run SimAnn with few iterations to brute force anchors
                    _viewModel.NumberIterationsGeneration /= 4;
                    _viewModel.NumberPathSamples /= 2;

                    var resampledInputPathForAnchors = PolylineHelper.SamplePolygon(_model.InputPath.Select(p => new Point(p.X, p.Y)).ToList(), _viewModel.NumberPathSamples, false);
                    var resampledOutputPathForAnchors = PolylineHelper.SamplePolygon(_model.OutputPath.Select(p => new Point(p.X, p.Y)).ToList(), _viewModel.NumberPathSamples, false);

                    SimulationModel bestModelForAnchor = null;
                    Edge bestEdgeForAnchors = null;

                    var startModel = new MetamaterialModel(_model);

                    //foreach (var cell in startModel.Cells)
                    //{
                    //    var edges = cell.CellEdges;
                    //    var edge = edges[_random.Next(edges.Count)];
                    
                        foreach (var edge in startModel.Edges)
                        {
                        //    if (cell.CellVertices.Contains(_model.InputVertex) || cell.CellVertices.Contains(_model.OutputVertex))
                        //{
                        //    Console.WriteLine(@"Skip edge " + _model.Edges.IndexOf(edge));
                        //}

                        _model = new MetamaterialModel(startModel);

                        _model.ClearAnchors();
                        _model.AddAnchor(edge.Vertex1.ToVector());
                        _model.AddAnchor(edge.Vertex2.ToVector());

                        //run SimAnn with few iterations to brute force anchors
                        var currentModelForAnchors = RunSimulatedAnnealing(resampledInputPathForAnchors, resampledOutputPathForAnchors, currentNumOptimizationScales, 1);

                        if (bestModelForAnchor == null)
                        {
                            bestModelForAnchor = currentModelForAnchors;
                            bestEdgeForAnchors = edge;
                        }
                        else
                        {
                            var resultDidImprove = SimulationModel.ResultDidImprove(currentModelForAnchors.MinError,
                                bestModelForAnchor.MinError, currentModelForAnchors.CurrentDof,
                                bestModelForAnchor.CurrentDof);

                            Console.WriteLine(@"Done for edge " + startModel.Edges.IndexOf(edge) + @" of " + _model.Edges.Count + @" at " + edge + @" with error " + currentModelForAnchors.MinError.ToString("F4") + @" vs " + bestModelForAnchor.MinError.ToString(@"F4"));
                            _presenter.ExportConfiguration(currentModelForAnchors.Model, "auto_solution_" + currentNumOptimizationScales + @"_" + FilePrefix + @"_" + _model.Edges.IndexOf(edge) + @".txt");

                            if (resultDidImprove)
                            {
                                bestModelForAnchor = currentModelForAnchors;
                                bestEdgeForAnchors = edge;

                                Console.WriteLine(@"Result did improve for edge " + startModel.Edges.IndexOf(edge) + @" at " + edge + @" with error " + bestModelForAnchor.MinError.ToString("F4"));
                            }
                        }
                    }

                    _model = new MetamaterialModel(bestModelForAnchor.Model);

                    Console.WriteLine(@"Best result for edge " + startModel.Edges.IndexOf(bestEdgeForAnchors) + @" of " + startModel.Edges.Count + @" at " + bestEdgeForAnchors + @" with error " + bestModelForAnchor.MinError.ToString("F4"));

                    _model.ClearAnchors();
                    _model.AddAnchor(bestEdgeForAnchors.Vertex1.ToVector());
                    _model.AddAnchor(bestEdgeForAnchors.Vertex2.ToVector());

                    _viewModel.NumberIterationsGeneration *= 4;
                    _viewModel.NumberPathSamples *= 2;
                }

                var resampledInputPath = PolylineHelper.SamplePolygon(_model.InputPath.Select(p => new Point(p.X, p.Y)).ToList(), _viewModel.NumberPathSamples, false);
                var resampledOutputPath = PolylineHelper.SamplePolygon(_model.OutputPath.Select(p => new Point(p.X, p.Y)).ToList(), _viewModel.NumberPathSamples, false);

                var bestModelInScale = RunSimulatedAnnealing(resampledInputPath, resampledOutputPath, currentNumOptimizationScales);

                if (bestModel != null)
                {
                    resultDidImproveAfterScale = SimulationModel.ResultDidImprove(bestModelInScale.MinError, bestModel.MinError, bestModelInScale.CurrentDof, bestModel.CurrentDof);

                    if (resultDidImproveAfterScale)
                    {
                        Console.WriteLine(@"Result did improve for scale " + currentNumOptimizationScales + @" with error " + bestModelInScale.MinError.ToString("F4"));

                        bestModel = bestModelInScale;
                        _model = new MetamaterialModel(bestModel.Model);
                    }
                    else
                    {
                        Console.WriteLine(@"Result did NOT improve for scale " + currentNumOptimizationScales + @" with error " + bestModelInScale.MinError.ToString("F4"));
                    }
                }
                else
                {
                    bestModel = bestModelInScale;
                    _model = new MetamaterialModel(bestModel.Model);
                    Console.WriteLine(@"set best model");
                }

                if (resultDidImproveAfterScale)
                {
                    _presenter.ExportConfiguration(bestModel.Model, "auto_solution_" + currentNumOptimizationScales + @"_" + FilePrefix);

                    currentNumOptimizationScales++;

                    if (currentNumOptimizationScales < _viewModel.MaxNumScalesOptimization)
                    {
                        _model.Scale(2, true);
                        Console.WriteLine(@"Scale up during hierarchical optimization for scale " + currentNumOptimizationScales);
                    }
                }
                else
                {
                    Console.WriteLine(@"Result did NOT improve for scale " + currentNumOptimizationScales + @". Reset to model with error " + bestModel.MinError.ToString("F4"));
                    _model = new MetamaterialModel(bestModel.Model);
                }
            }

            var duration = DateTime.Now - startTime;
            Console.WriteLine(@"Done with optimization. Duration " + duration.ToString("g") + @". " +
                              @"Best result has error " + bestModel.MinError.ToString("F4") + @" after num of scales " + currentNumOptimizationScales);

            _model.InputPath = originalInputPath;
            _model.OutputPath = originalOutputPath;

            _viewModel.Model = _model;
            _presenter.Model = _model;

            Console.WriteLine("length i " + originalInputPath.Count + " / length o " + originalOutputPath.Count);

            var finalOptimizationScale = currentNumOptimizationScales - 1;
            var scale = 2 * finalOptimizationScale;
            if (scale > 0)
                _viewModel.Model.ScalePath(scale);

            _viewModel.InputPath = CoordinateConverter.ConvertGlobalToScreenCoordinates(_viewModel.Model.InputPath);
            _viewModel.OutputPath = CoordinateConverter.ConvertGlobalToScreenCoordinates(_viewModel.Model.OutputPath);

            //_simModel.ViewModel.InputPathDistance = normalizedInputError;
            //_simModel.ViewModel.OutputPathDistance = normalizedOutputError;

            _model.UpdateConstraintsGraph();

            //if (Logger != null)
            //{
            //    _presenter.ExportCurrentConfiguration("_auto_" + currentNumOptimizationScales + "_solution");
            //}

            Application.Current.Dispatcher.Invoke(_viewModel.Redraw);
        }

        private SimulationModel RunSimulatedAnnealing(List<Vector> inputPath, List<Vector> outputPath, int currentScale, int num_opti_restarts = NUM_MAX_OPTI_RESTARTS)
        {
            var currentNumOptimizationRuns = 0;
            var resultDidImproveAfterRun = true;

            SimulationModel bestModel = null;

            while (resultDidImproveAfterRun && currentNumOptimizationRuns < num_opti_restarts)
            {
                var modelsInRun = new List<SimulationModel>();

                for (int k = 0; k < NUM_PARALLEL_OPTI_RUNS; k++)
                {
                    var model = new SimulationModel(_model, _viewModel) { EnableSimulatedAnnealing = true };

                    if (bestModel != null)
                    {
                        model.AssignValues(bestModel);
                    }

                    model.Model.InputPath = new List<Vector>(inputPath);
                    model.Model.OutputPath = new List<Vector>(outputPath);

                    modelsInRun.Add(model);
                }

                foreach (var currentModel in modelsInRun)
                {
                    Console.WriteLine(@"Run for scale " + currentScale + @" with # of points " +
                                      currentModel.Model.InputPath.Count + @" in run " + currentNumOptimizationRuns);

                    var optimizer = new IterativeCellOptimizationController(currentModel);

                    if (Logger != null)
                    {
                        optimizer.Logger = Logger;
                        Logger.AddOrChangePrefix(
                            currentScale.ToString("D") + "_" + currentNumOptimizationRuns.ToString("D") + "_" +
                            modelsInRun.IndexOf(currentModel), this);
                    }

                    optimizer.StartOptimization();
                }

                var bestModelInRun = modelsInRun[0];

                foreach (var model in modelsInRun)
                {
                    if (model.MinError < bestModelInRun.MinError)
                    {
                        bestModelInRun = model;
                    }
                }

                Console.WriteLine(@"Best result in run " + currentNumOptimizationRuns + @" has error " +
                                  bestModelInRun.MinError + @" and is index " + modelsInRun.IndexOf(bestModelInRun));

                if (bestModel != null)
                {
                    resultDidImproveAfterRun = SimulationModel.ResultDidImprove(bestModelInRun.MinError, bestModel.MinError, bestModelInRun.CurrentDof, bestModel.CurrentDof);

                    if (resultDidImproveAfterRun)
                    {
                        bestModel = bestModelInRun;
                        _model = bestModel.Model;
                        _viewModel.Model = _model;
                    }
                }
                else
                {
                    bestModel = bestModelInRun;
                    _model = bestModel.Model;
                    _viewModel.Model = _model;
                }

                currentNumOptimizationRuns++;
            }

            return bestModel;
        }
    }
}