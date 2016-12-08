namespace WorkoutWotch.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using Genesis.Ensure;
    using ReactiveUI;
    using WorkoutWotch.Models;

    public sealed class ExerciseViewModel : ReactiveObject, ISupportsActivation
    {
        private readonly ViewModelActivator activator;
        private readonly Exercise model;
        private ExecutionContext executionContext;
        private TimeSpan progressTimeSpan;
        private bool isActive;
        private double progress;

        public ExerciseViewModel(IScheduler scheduler, Exercise model, IObservable<ExecutionContext> executionContext)
        {
            Ensure.ArgumentNotNull(scheduler, nameof(scheduler));
            Ensure.ArgumentNotNull(model, nameof(model));
            Ensure.ArgumentNotNull(executionContext, nameof(executionContext));

            this.activator = new ViewModelActivator();
            this.model = model;

            this
                .WhenAny(
                    x => x.Duration,
                    x => x.ProgressTimeSpan,
                    (duration, progressTimeSpan) => progressTimeSpan.Value.TotalMilliseconds / duration.Value.TotalMilliseconds)
                .Select(progressRatio => double.IsNaN(progressRatio) || double.IsInfinity(progressRatio) ? 0d : progressRatio)
                .Select(progressRatio => Math.Min(1d, progressRatio))
                .Select(progressRatio => Math.Max(0d, progressRatio))
                .ObserveOn(scheduler)
                .Subscribe(x => this.Progress = x);

            Observable
                .CombineLatest(
                    this
                        .WhenAnyValue(x => x.ExecutionContext)
                        .Select(ec => ec == null ? Observable<TimeSpan>.Never : ec.WhenAnyValue(x => x.SkipAhead))
                        .Switch(),
                    this
                        .WhenAnyValue(x => x.ExecutionContext)
                        .Select(ec => ec == null ? Observable<Exercise>.Never : ec.WhenAnyValue(x => x.CurrentExercise))
                        .Switch(),
                    (skip, current) => skip == TimeSpan.Zero && current == this.model)
                .ObserveOn(scheduler)
                .Subscribe(x => this.IsActive = x);

            this
                .WhenActivated(
                    disposables =>
                    {
                        executionContext
                            .ObserveOn(scheduler)
                            .Subscribe(x => this.ExecutionContext = x)
                            .AddTo(disposables);

                        this
                            .WhenAnyValue(x => x.ExecutionContext)
                            .Select(
                                ec =>
                                    ec == null
                                        ? Observable.Return(TimeSpan.Zero)
                                        : ec
                                            .WhenAnyValue(x => x.CurrentExerciseProgress)
                                            .Where(_ => ec.CurrentExercise == this.model)
                                            .StartWith(TimeSpan.Zero))
                            .Switch()
                            .ObserveOn(scheduler)
                            .Subscribe(x => this.ProgressTimeSpan = x)
                            .AddTo(disposables);
                    });
        }

        public ViewModelActivator Activator => this.activator;

        public Exercise Model => this.model;

        public string Name => this.model.Name;

        public TimeSpan Duration => this.model.Duration;

        public TimeSpan ProgressTimeSpan
        {
            get { return this.progressTimeSpan; }
            private set { this.RaiseAndSetIfChanged(ref this.progressTimeSpan, value); }
        }

        public double Progress
        {
            get { return this.progress; }
            private set { this.RaiseAndSetIfChanged(ref this.progress, value); }
        }

        public bool IsActive
        {
            get { return this.isActive; }
            private set { this.RaiseAndSetIfChanged(ref this.isActive, value); }
        }

        private ExecutionContext ExecutionContext
        {
            get { return this.executionContext; }
            set { this.RaiseAndSetIfChanged(ref this.executionContext, value); }
        }
    }
}