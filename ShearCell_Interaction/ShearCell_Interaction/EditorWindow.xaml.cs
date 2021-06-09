using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ShearCell_Interaction.Helper;
using ShearCell_Interaction.Model;
using ShearCell_Interaction.Optimisation;
using ShearCell_Interaction.Presenter;
using ShearCell_Interaction.Randomizer;
using ShearCell_Interaction.Simulation;
using ShearCell_Interaction.Test;
using ShearCell_Interaction.View;

namespace ShearCell_Interaction
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : Window
    {
        private readonly MetamaterialPresenter _presenter;
        private readonly ViewModel _viewModel;

        private DateTime _lastMove;
        private Vector _lastMousePosition;
        private Vector _cellAreaStartPosition;
        private bool _isDraggingCellArea;
        private bool _isMouseDown;
        private bool _isShiftDown;
        private bool _isCtrlDown;
        private bool _isDraggingCanvas;
        private bool _justChangedPathSelection;

        public EditorWindow()
        {
            var model = new MetamaterialModel();
            _viewModel = new ViewModel(model);
            _presenter = new MetamaterialPresenter(model, _viewModel);

            Height = ViewModel.WindowHeight + 120; //70 for controls bar + window frame
            Width = ViewModel.WindowWidth;

            DataContext = _viewModel;
            InitializeComponent();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void DrawingCanvasStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {

        }

        private void OnDrawingButtonUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void OnDrawingButtonClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnOptimizedPathSimulationClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnSimulationPathSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OnSimulationPlayPause(object sender, RoutedEventArgs e)
        {

        }

        private void OnSimulationStepBack(object sender, RoutedEventArgs e)
        {

        }

        private void OnSimulationStepForward(object sender, RoutedEventArgs e)
        {

        }
    }
}
