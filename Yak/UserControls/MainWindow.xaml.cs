﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Yak.Events;
using Yak.ViewModel;
using GalaSoft.MvvmLight.Threading;

namespace Yak.UserControls
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        #endregion

        #region Methods

        #region Method -> OnClosing
        /// <summary>
        /// Do actions when closing
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">CancelEventArgs</param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Unsubscribe events
            Loaded -= OnLoaded;

            // Stop playing and downloading a movie if any
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                // Unsubscribe events
                vm.ConnectionError -= OnConnectionInError;
                vm.DownloadingMovie -= OnDownloadingMovie;
                vm.StoppedDownloadingMovie -= OnStoppedDownloadingMovie;
                vm.LoadedMovie -= OnLoadedMovie;
                vm.LoadingMovie -= OnLoadingMovie;
                vm.BufferedMovie -= OnBufferedMovie;
                vm.LoadingMovieProgress -= OnLoadingMovieProgress;
                vm.LoadedTrailer -= OnLoadedTrailer;
                vm.LoadingTrailer -= OnLoadingTrailer;

                if (vm.IsDownloadingMovie)
                {
                    vm.StopDownloadingMovie();
                }

                foreach (var tab in vm.MoviesViewModelTabs)
                {
                    var moviesViewModelTab = tab as MoviesViewModel;
                    moviesViewModelTab?.Cleanup();
                }
            }            

            ViewModelLocator.Cleanup();
        }
        #endregion

        #region Method -> OnLoaded
        /// <summary>
        /// Do actions when loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">RoutedEventArgs</param>
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.ConnectionError += OnConnectionInError;
                vm.DownloadingMovie += OnDownloadingMovie;
                vm.StoppedDownloadingMovie += OnStoppedDownloadingMovie;
                vm.LoadedMovie += OnLoadedMovie;
                vm.LoadingMovie += OnLoadingMovie;
                vm.BufferedMovie += OnBufferedMovie;
                vm.LoadingMovieProgress += OnLoadingMovieProgress;
                vm.LoadedTrailer += OnLoadedTrailer;
                vm.LoadingTrailer += OnLoadingTrailer;
            }
        }
        #endregion

        #region Method -> OnLoadingTrailer
        /// <summary>
        /// Hide movie content page when trailer is loading
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">TrailerLoadedEventArgs</param>
        void OnLoadingTrailer(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                #region Fade in content opacity
                var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                var opacityEasingFunction = new PowerEase();
                opacityEasingFunction.EasingMode = EasingMode.EaseInOut;
                var startOpacityEasing = new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0));
                var endOpacityEasing = new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0),
                    opacityEasingFunction);
                opacityAnimation.KeyFrames.Add(startOpacityEasing);
                opacityAnimation.KeyFrames.Add(endOpacityEasing);

                Content.BeginAnimation(OpacityProperty, opacityAnimation);
                #endregion
            });
        }
        #endregion

        #region Method -> OnLoadedTrailer
        /// <summary>
        /// Play the trailer when loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">TrailerLoadedEventArgs</param>
        private void OnLoadedTrailer(object sender, TrailerLoadedEventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (!e.InErrorOrCancelled)
                {
                    var vm = DataContext as MainViewModel;
                    if (vm != null)
                    {
                        vm.Trailer = new MediaPlayerViewModel(Helpers.Constants.MediaType.Trailer, new Uri(e.TrailerUrl));
                        vm.IsMovieTrailerLoading = false;
                    }
                }
                else
                {
                    #region Fade in content opacity
                    var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                    opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                    var opacityEasingFunction = new PowerEase();
                    opacityEasingFunction.EasingMode = EasingMode.EaseInOut;
                    var startOpacityEasing = new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(0));
                    var endOpacityEasing = new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0),
                        opacityEasingFunction);
                    opacityAnimation.KeyFrames.Add(startOpacityEasing);
                    opacityAnimation.KeyFrames.Add(endOpacityEasing);

                    Content.BeginAnimation(OpacityProperty, opacityAnimation);
                    #endregion
                }
            });
        }

        #endregion

        #region Method -> OnLoadingMovieProgress
        /// <summary>
        /// Report progress when a movie is loading
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">MovieLoadingProgressEventArgs</param>
        private void OnLoadingMovieProgress(object sender, MovieLoadingProgressEventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (ProgressBar.Visibility == Visibility.Collapsed && e.Progress <= Helpers.Constants.MinimumBufferingBeforeMoviePlaying)
                {
                    ProgressBar.Visibility = Visibility.Visible;
                }

                if (StopLoadingMovieButton.Visibility == Visibility.Collapsed)
                {
                    StopLoadingMovieButton.Visibility = Visibility.Visible;
                }

                if (LoadingText.Visibility == Visibility.Collapsed)
                {
                    LoadingText.Visibility = Visibility.Visible;
                }

                ProgressBar.Value = e.Progress;

                // The percentage here is related to the buffering progress
                var percentage = e.Progress/Helpers.Constants.MinimumBufferingBeforeMoviePlaying*100.0;

                if (percentage >= 100.0)
                {
                    var vm = DataContext as MainViewModel;
                    if (vm != null)
                    {
                        foreach (var tab in vm.MoviesViewModelTabs)
                        {
                            var moviesViewModel = tab as MediaPlayerViewModel;
                            if (moviesViewModel != null)
                            {
                                LoadingText.Text = "Currently playing : " + moviesViewModel.Tab.TabName;
                            }
                        }
                    }
                }
                else
                {
                    if (e.DownloadRate >= 1000)
                    {
                        LoadingText.Text = "Buffering : " + Math.Round(percentage, 0) + "%" + " ( " +
                                           e.DownloadRate/1000 + " MB/s)";
                    }
                    else
                    {
                        LoadingText.Text = "Buffering : " + Math.Round(percentage, 0) + "%" + " ( " + e.DownloadRate +
                                           " kB/s)";
                    }
                }
            });
        }
        #endregion

        #region Method -> OnDownloadingMovie
        /// <summary>
        /// Fade in the movie page's opacity when a movie is loading
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnDownloadingMovie(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                #region Fade in opacity
                var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                var opacityEasingFunction = new PowerEase();
                opacityEasingFunction.EasingMode = EasingMode.EaseInOut;
                var startOpacityEasing = new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0));
                var endOpacityEasing = new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0),
                    opacityEasingFunction);
                opacityAnimation.KeyFrames.Add(startOpacityEasing);
                opacityAnimation.KeyFrames.Add(endOpacityEasing);

                Content.BeginAnimation(OpacityProperty, opacityAnimation);
                #endregion
            });
        }
        #endregion

        #region Method -> OnLoadingMovie
        /// <summary>
        /// Open the movie flyout when a movie is selected from the main interface, show progress and hide content
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        void OnLoadingMovie(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                MoviePage.IsOpen = true;

                MovieProgressBar.Visibility = Visibility.Visible;
                MovieContainer.Visibility = Visibility.Collapsed;

                MovieContainer.Opacity = 0.0;
            });
        }
        #endregion

        #region Method -> OnLoadedMovie
        /// <summary>
        /// Show content and hide progress when a movie is loaded
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        void OnLoadedMovie(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                MovieProgressBar.Visibility = Visibility.Collapsed;
                MovieContainer.Visibility = Visibility.Visible;

                #region Fade in opacity

                var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                var opacityEasingFunction = new PowerEase();
                opacityEasingFunction.EasingMode = EasingMode.EaseInOut;
                var startOpacityEasing = new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(0));
                var endOpacityEasing = new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0),
                    opacityEasingFunction);
                opacityAnimation.KeyFrames.Add(startOpacityEasing);
                opacityAnimation.KeyFrames.Add(endOpacityEasing);

                MovieContainer.BeginAnimation(OpacityProperty, opacityAnimation);

                #endregion
            });
        }
        #endregion

        #region Method -> OnBufferedMovie

        /// <summary>
        /// Play the movie when buffered
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event args</param>
        private void OnBufferedMovie(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                MoviePage.IsOpen = false;
                ProgressBar.Visibility = Visibility.Collapsed;
                StopLoadingMovieButton.Visibility = Visibility.Collapsed;
                LoadingText.Visibility = Visibility.Collapsed;
            });
        }

        #endregion

        #region Method -> OnConnectionInError

        /// <summary>
        /// Open the popup when a connection error has occured
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event args</param>
        private async void OnConnectionInError(object sender, EventArgs e)
        {
            // Set and open a MetroDialog to inform that a connection error occured
            var settings = new MetroDialogSettings();
            settings.ColorScheme = MetroDialogColorScheme.Theme;
            var result = await
                this.ShowMessageAsync("Internet connection error.",
                    "You seem to have an internet connection error. Please retry.",
                    MessageDialogStyle.Affirmative, settings);

            // Catch the response's user (when clicked OK)
            if (result == MessageDialogResult.Affirmative)
            {
                // Close the movie page
                if (MoviePage.IsOpen)
                {
                    MoviePage.IsOpen = false;
                }
            }
        }

        #endregion

        #region Method -> OnStoppedDownloadingMovie
        /// <summary>
        /// Close the player and go back to the movie page when the downloading of the movie has stopped
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">EventArgs</param>
        private void OnStoppedDownloadingMovie(object sender, EventArgs e)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                StopLoadingMovieButton.Visibility = Visibility.Collapsed;
                LoadingText.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0.0;

                MoviePage.IsOpen = true;

                #region Fade out opacity

                var opacityAnimation = new DoubleAnimationUsingKeyFrames();
                opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.5));
                var opacityEasingFunction = new PowerEase();
                opacityEasingFunction.EasingMode = EasingMode.EaseInOut;
                var startOpacityEasing = new EasingDoubleKeyFrame(0.0, KeyTime.FromPercent(0));
                var endOpacityEasing = new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0),
                    opacityEasingFunction);
                opacityAnimation.KeyFrames.Add(startOpacityEasing);
                opacityAnimation.KeyFrames.Add(endOpacityEasing);

                Content.BeginAnimation(OpacityProperty, opacityAnimation);

                #endregion
            });
        }
        #endregion

        #endregion
    }
}