﻿using LANPaint_vNext.Model;
using LANPaint_vNext.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;


namespace LANPaint_vNext.ViewModels
{
    public class PaintViewModel : BindableBase
    {
        private bool _isEraser;
        private StrokeCollection _strokes;
        private Color _backgroundColor;

        public bool IsEraser
        {
            get { return _isEraser; }
            set
            {
                SetProperty(ref _isEraser, value);
            }
        }
        public StrokeCollection Strokes
        {
            get => _strokes;
            set
            {
                SetProperty(ref _strokes, value);
            }
        }
        public Color Background
        {
            get => _backgroundColor;
            set
            {
                SetProperty(ref _backgroundColor, value);
            }
        }
        public bool BroadcastEnabled { get; set; }
        public bool ReceiveEnabled { get; set; }


        public RelayCommand ClearCommand { get; private set; }
        public RelayCommand ChoosePenCommand { get; private set; }
        public RelayCommand ChooseEraserCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand OpenCommand { get; private set; }

        private IDialogWindowService _dialogService;
        private UDPBroadcastService _broadcastService;

        public PaintViewModel(IDialogWindowService dialogService)
        {
            _dialogService = dialogService;
            _broadcastService = new UDPBroadcastService();

            Strokes = new StrokeCollection();
            Strokes.StrokesChanged += OnStrokesCollectionChanged;
            ClearCommand = new RelayCommand(async (param) => await ClearCommandHandler(param), param => Strokes.Count > 0);
            ChoosePenCommand = new RelayCommand(param => IsEraser = false, param => IsEraser);
            ChooseEraserCommand = new RelayCommand(param => IsEraser = true, param => !IsEraser);
            SaveCommand = new RelayCommand(OnSaveExecuted);
            OpenCommand = new RelayCommand(OnOpenExecuted);
        }

        private async ValueTask ClearCommandHandler(object obj)
        {
            Strokes.Clear();
            if(BroadcastEnabled)
            {
                var info = new DrawingInfo(ARGBColor.Default, new SerializableStroke(), IsEraser, true);
                var serializer = new BinarySerializerService();
                var bytes = serializer.Serialize(info);
                await _broadcastService.SendAsync(bytes);
            }
        }

        private void OnSaveExecuted(object param)
        {
            var savePath = _dialogService.SaveFileDialog();
            //TODO: Save all drawing data into object, serialize it and save to file
        }

        private void OnOpenExecuted(object param)
        {
            var openPath = _dialogService.OpenFileDialog();
            //TODO: Add suggestion to save current work in case board not empty???
            //TODO: Read file, deserialize to object and apply to current border
        }

        private async void OnStrokesCollectionChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Added.Count > 0)
            {
                Debug.WriteLine($"Strokes added: {e.Added.Count}");
                if (BroadcastEnabled)
                {
                    Debug.WriteLine($"Sending stroke...");
                    foreach (var stroke in e.Added)
                    {
                        var attr = new StrokeAttributes { Color = ARGBColor.FromColor(stroke.DrawingAttributes.Color), Height = stroke.DrawingAttributes.Height,
                                                          Width = stroke.DrawingAttributes.Width };
                        var points = new List<Point>();
                        foreach (var point in stroke.StylusPoints)
                        {
                            points.Add(point.ToPoint());
                        }

                        var serializableStroke = new SerializableStroke(attr, points);
                        var info = new DrawingInfo(Background, serializableStroke, IsEraser);
                        var serializer = new BinarySerializerService();
                        var bytes = serializer.Serialize(info);
                        await _broadcastService.SendAsync(bytes);
                    }
                }
            }
            if (e.Removed.Count > 0)
            {
                Debug.WriteLine($"Strokes removed: {e.Removed.Count}");
            }
        }
    }
}
