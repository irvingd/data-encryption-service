using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DataEncryptionService.Telemetry
{
    public class TelemetrySpanMeasure : IDisposable
    {
        private readonly string _name;
        private readonly string _nameKey;
        private readonly Stopwatch _stopWatch;
        private readonly ICollection<TelemetrySpan> _collectedSpans;
        private readonly IEnumerable<string> _tags;
        private readonly int _nestLevel;

        //private readonly Action<IEnumerable<TelemetrySpan>> _onSpanComplete;
        //private readonly TelemetrySpanMeasure _parentSpan;
        //private ConcurrentBag<TelemetrySpan> childrenSpan = new ConcurrentBag<TelemetrySpan>();

        private bool disposedValue;

        public IEnumerable<TelemetrySpan> StopAndGetSpans()
        {
            _stopWatch.Stop();
            _collectedSpans.Add(new TelemetrySpan()
            {
                Name = _name,
                NameKey = _nameKey,
                ElapsedTicks = _stopWatch.ElapsedTicks,
                ElapsedMs = _stopWatch.ElapsedMilliseconds,
                Tags = _tags?.ToList(), // this makes a copy, if tags were set
                NestLevel = _nestLevel
            });

            return _collectedSpans;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopAndGetSpans();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private TelemetrySpanMeasure(string name, string nameKey, ICollection<TelemetrySpan> collectedSpans, TelemetrySpanMeasure parentSpan, IEnumerable<string> tags)
        {
            _name = name;
            _nameKey = nameKey;
            _collectedSpans = collectedSpans;
            _tags = tags;
            _nestLevel = 1 + (null == parentSpan ? 0 : parentSpan._nestLevel);

            _stopWatch = Stopwatch.StartNew();
        }

        static public TelemetrySpanMeasure Start(string name, string nameKey, ICollection<TelemetrySpan> collectedSpans, TelemetrySpanMeasure parentSpan = null, IEnumerable<string> tags = null)
        {
            return new TelemetrySpanMeasure(name, nameKey, collectedSpans, parentSpan, tags);
        }
    }
}