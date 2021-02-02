﻿using LANPaint.Model;
using LANPaint.Services.Network;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using LANPaint.Dialogs.Service;

namespace LANPaint.ViewModels
{
    public class SettingsViewModel : DialogViewModelBase<UDPSettings>, IDisposable
    {
        private NICInfo _selectedNic;

        public ObservableCollection<NICInfo> Interfaces { get; }
        public NICInfo SelectedNic
        {
            get => _selectedNic;
            set
            {
                if (!Interfaces.Contains(value)) return;
                _selectedNic = value;
            }
        }
        public int Port { get; set; }

        public RelayCommand<IDialogWindow> OkCommand { get; }
        public RelayCommand<IDialogWindow> CancelCommand { get; }

        public SettingsViewModel() : base("Settings")
        {
            Interfaces = new ObservableCollection<NICInfo>();
            UpdateInterfaceCollection();

#warning Remove this
            SelectedNic = Interfaces.First();

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChangedHandler;
            NetworkChange.NetworkAddressChanged += NetworkAddressChangedHandler;
            OkCommand = new RelayCommand<IDialogWindow>(OnOkCommand);
            CancelCommand = new RelayCommand<IDialogWindow>(OnCancelCommand);
        }

        private void OnOkCommand(IDialogWindow window)
        {
            DialogResult = new UDPSettings(SelectedNic.IpAddress, Port);
            CloseDialogWithResult(window, DialogResult);
        }

        private void OnCancelCommand(IDialogWindow window)
        {
            DialogResult = new UDPSettings(IPAddress.None);
            CloseDialogWithResult(window, DialogResult);
        }
        
        private void UpdateInterfaceCollection()
        {
            var interfaces = NICHelper.GetInterfaces().Select(nic => new NICInfo()
            {
                Name = nic.Name,
                IpAddress = nic.GetIPProperties().UnicastAddresses.First(information =>
                    information.Address.AddressFamily == AddressFamily.InterNetwork).Address,
                IsReadyToUse = nic.OperationalStatus == OperationalStatus.Up
            });

            Interfaces.Clear();
            foreach (var nicInfo in interfaces)
            {
                Interfaces.Add(nicInfo);
            }
        }

        private void NetworkAddressChangedHandler(object sender, EventArgs e)
        {
            UpdateInterfaceCollection();
        }

        private void NetworkAvailabilityChangedHandler(object sender, NetworkAvailabilityEventArgs e)
        {
            UpdateInterfaceCollection();
        }

        public void Dispose()
        {
            NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChangedHandler;
            NetworkChange.NetworkAddressChanged -= NetworkAddressChangedHandler;
        }
    }
}