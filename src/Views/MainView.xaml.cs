/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Views
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Navigation;

    using Autofac;

    using Caliburn.Micro;

    using MahApps.Metro.Controls;

    using MimeKit;

    using Papercut.Core;
    using Papercut.Core.Events;
    using Papercut.Core.Helper;
    using Papercut.Core.Message;
    using Papercut.Events;
    using Papercut.Helpers;
    using Papercut.Properties;
    using Papercut.Services;
    using Papercut.ViewModels;

    using Serilog;

    using Action = System.Action;
    using Application = System.Windows.Application;
    using DataFormats = System.Windows.DataFormats;
    using DataObject = System.Windows.DataObject;
    using DragDropEffects = System.Windows.DragDropEffects;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using ListBox = System.Windows.Controls.ListBox;
    using MessageBox = System.Windows.MessageBox;
    using MouseEventArgs = System.Windows.Input.MouseEventArgs;
    using Point = System.Windows.Point;
    using ScrollBar = System.Windows.Controls.Primitives.ScrollBar;

    /// <summary>
    ///     Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : MetroWindow
    {
        readonly Func<ForwardViewModel> _forwardViewModelFactory;

        #region Fields

        readonly object _deleteLockObject = new object();

        Point? _dragStartPoint;

        IDisposable _loadingDisposable;

        public ILogger Logger { get; set; }

        public MimeMessageLoader MimeMessageLoader { get; set; }

        public AppResourceLocator ResourceLocator { get; set; }

        public IWindowManager WindowManager { get; set; }

        public IPublishEvent PublishEvent { get; set; }

        public MessageRepository MessageRepository { get; set; }

        #endregion

        #region Constructors and Destructors

        public MainView(
            MessageRepository messageRepository,
            MimeMessageLoader mimeMessageLoader,
            AppResourceLocator resourceLocator,
            Func<ForwardViewModel> forwardViewModelFactory,
            IWindowManager windowManager,
            IPublishEvent publishEvent,
            ILogger logger)
        {
            _forwardViewModelFactory = forwardViewModelFactory;
            MessageRepository = messageRepository;
            MimeMessageLoader = mimeMessageLoader;
            ResourceLocator = resourceLocator;
            WindowManager = windowManager;
            PublishEvent = publishEvent;
            Logger = logger;

            InitializeComponent();
        }

        #endregion
    }
}