﻿namespace WorkoutWotch.Models.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Reactive;
    using Genesis.Ensure;
    using WorkoutWotch.Services.Contracts.Audio;
    using WorkoutWotch.Services.Contracts.Delay;

    public sealed class MetronomeAction : IAction
    {
        private readonly IImmutableList<MetronomeTick> ticks;
        private readonly SequenceAction innerAction;

        public MetronomeAction(IAudioService audioService, IDelayService delayService, IEnumerable<MetronomeTick> ticks)
        {
            Ensure.ArgumentNotNull(audioService, nameof(audioService));
            Ensure.ArgumentNotNull(delayService, nameof(delayService));
            Ensure.ArgumentNotNull(ticks, nameof(ticks));

            this.ticks = ticks.ToImmutableList();
            this.innerAction = new SequenceAction(GetInnerActions(audioService, delayService, this.ticks));
        }

        public TimeSpan Duration => this.innerAction.Duration;

        public IImmutableList<MetronomeTick> Ticks => this.ticks;

        public IObservable<Unit> Execute(ExecutionContext context)
        {
            Ensure.ArgumentNotNull(context, nameof(context));

            return this
                .innerAction
                .Execute(context);
        }

        private static IEnumerable<IAction> GetInnerActions(IAudioService audioService, IDelayService delayService, IEnumerable<MetronomeTick> ticks)
        {
            foreach (var tick in ticks)
            {
                yield return new WaitAction(delayService, tick.PeriodBefore);

                switch (tick.Type)
                {
                    case MetronomeTickType.Click:
                        yield return new DoNotAwaitAction(new AudioAction(audioService, "MetronomeClick"));
                        break;
                    case MetronomeTickType.Bell:
                        yield return new DoNotAwaitAction(new AudioAction(audioService, "MetronomeBell"));
                        break;
                }
            }
        }
    }
}