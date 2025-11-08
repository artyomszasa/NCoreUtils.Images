using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using ImageMagick.Configuration;
using Microsoft.Extensions.Logging;
using NCoreUtils.Images.Internal;
using LogEvents = ImageMagick.LogEventTypes;

namespace NCoreUtils.Images.ImageMagick;

public class ImageProvider : IImageProvider
{
#if NET6_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ImageMagick.ExifTagDescriptionAttribute", "Magick.NET.Core")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AssemblyTitleAttribute))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AssemblyFileVersionAttribute))]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static ImageProvider()
    {
        /* noop: trimming only */
    }
#endif

    private static BigInteger ParseSize(ReadOnlySpan<char> source)
    {
        var numEnd = 0;
        while (source.Length > numEnd && (source[numEnd] == '.' || char.IsDigit(source[numEnd])))
        {
            ++numEnd;
        }
        if (numEnd == source.Length)
        {
            return decimal.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out var i0)
                ? (BigInteger)Math.Round(i0)
                : default;
        }
        var i = numEnd == 0
            ? default
            : decimal.Parse(source[..numEnd], NumberStyles.Float, CultureInfo.InvariantCulture);
        var rest = source[numEnd..];
        // if (rest.Length == 1)
        // {
        //     // if (rest[0] == 'B' || rest[0] == 'b')
        //     // {
        //     //     return i;
        //     // }
        //     return i;
        // }
        if (rest.Length == 3)
        {
            if (MemoryExtensions.Equals("KiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m);
            }
            if (MemoryExtensions.Equals("MiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m * 1024m);
            }
            if (MemoryExtensions.Equals("GiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m * 1024m * 1024m);
            }
            if (MemoryExtensions.Equals("TiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m * 1024m * 1024m * 1024m);
            }
            if (MemoryExtensions.Equals("PiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m * 1024m * 1024m * 1024m * 1024m);
            }
            if (MemoryExtensions.Equals("EiB", rest, StringComparison.InvariantCultureIgnoreCase))
            {
                return (BigInteger)Math.Round(i * 1024m * 1024m * 1024m * 1024m * 1024m * 1024m);
            }
        }
        return (BigInteger)Math.Round(i);
    }

    private static bool TryParseSizes(ReadOnlySpan<char> source, out (Range bucket, BigInteger diff, BigInteger used, BigInteger total) sizes)
    {
        var iColon = source.IndexOf(':');
        if (-1 == iColon)
        {
            sizes = default;
            return false;
        }
        var iStart = iColon + 1;
        if (iStart >= source.Length)
        {
            sizes = default;
            return false;
        }
        while (char.IsWhiteSpace(source[iStart]))
        {
            if (++iStart >= source.Length)
            {
                sizes = default;
                return false;
            }
        }
        var iEnd = iStart + source[iStart..].IndexOf('/');
        if (-1 == iEnd)
        {
            sizes = default;
            return false;
        }
        var diff = ParseSize(source[iStart..iEnd]);
        iStart = iEnd + 1;
        if (iStart >= source.Length)
        {
            sizes = default;
            return false;
        }
        iEnd = iStart + source[iStart..].IndexOf('/');
        if (-1 == iEnd)
        {
            sizes = default;
            return false;
        }
        var used = ParseSize(source[iStart..iEnd]);
        iStart = iEnd + 1;
        if (iStart >= source.Length)
        {
            sizes = default;
            return false;
        }
        var total = ParseSize(source[iStart..]);
        sizes = (new Range(Index.Start, new Index(iColon)), diff, used, total);
        return true;
    }

    private static LogLevel GetEventLogLevel(LogEvents e)
    {
        if (e == LogEvents.Trace)
        {
            return LogLevel.Trace;
        }
        if (e == LogEvents.Exception)
        {
            return LogLevel.Error;
        }
        if (e == LogEvents.Deprecate)
        {
            return LogLevel.Warning;
        }
        return LogLevel.Debug;
    }

    private static void DisableImageMagickNativeLogging()
    {
        var config = ConfigurationFiles.Default;
        config.Log.Data = @"<logmap><log events=""None""/><log output=""none""/></logmap>";
        MagickNET.Initialize(config);
    }

    private static void DisableManagedLogging()
    {
        MagickNET.SetLogEvents(LogEvents.None);
    }

    private readonly ResourceUsage _usage = new();

    public long MemoryLimit
    {
        get => (long)ResourceLimits.Memory;
        set => ResourceLimits.Memory = (ulong)value;
    }

    public ResourceUsageData Usage
        => _usage.Snapshot();

    public IImageMagickImageProviderConfiguration? Configuration { get; }

    public ImageProvider(ILoggerFactory? loggerFactory = default, IImageMagickImageProviderConfiguration? configuration = default)
    {
        Configuration = configuration;
        // disable default logging
        DisableImageMagickNativeLogging();
        // configure .NET logging
        if (configuration?.EnableLogging ?? true)
        {
            if (loggerFactory is not null)
            {
                // enable .NET logging
                EnableDetailedManagedLogging(loggerFactory);
            }
            else if (configuration?.EnableResourceTracking ?? true)
            {
                EnableResourceManagedLogging();
            }
        }
        else if (configuration?.EnableResourceTracking ?? true)
        {
            EnableResourceManagedLogging();
        }
        else
        {
            DisableManagedLogging();
        }
    }

    async ValueTask<IImage> IImageProvider.FromStreamAsync(Stream source, CancellationToken cancellationToken)
        => await FromStreamSyncOrAsync(source, cancellationToken);

    private void EnableDetailedManagedLogging(ILoggerFactory loggerFactory)
    {
        var defaultLogger = loggerFactory.CreateLogger(nameof(MagickNET));
        var loggers = new Dictionary<LogEvents, ILogger>
        {
            { LogEvents.Accelerate, loggerFactory.CreateLogger(LogCategories.Accelerate) },
            { LogEvents.Annotate, loggerFactory.CreateLogger(LogCategories.Annotate) },
            { LogEvents.Blob, loggerFactory.CreateLogger(LogCategories.Blob) },
            { LogEvents.Cache, loggerFactory.CreateLogger(LogCategories.Cache) },
            { LogEvents.Coder, loggerFactory.CreateLogger(LogCategories.Coder) },
            { LogEvents.Configure, loggerFactory.CreateLogger(LogCategories.Configure) },
            { LogEvents.Draw, loggerFactory.CreateLogger(LogCategories.Draw) },
            { LogEvents.Image, loggerFactory.CreateLogger(LogCategories.Image) },
            { LogEvents.Locale, loggerFactory.CreateLogger(LogCategories.Locale) },
            { LogEvents.Module, loggerFactory.CreateLogger(LogCategories.Module) },
            { LogEvents.Pixel, loggerFactory.CreateLogger(LogCategories.Pixel) },
            { LogEvents.Policy, loggerFactory.CreateLogger(LogCategories.Policy) },
            { LogEvents.Resource, loggerFactory.CreateLogger(LogCategories.Resource) },
            { LogEvents.Transform, loggerFactory.CreateLogger(LogCategories.Transform) },
            { LogEvents.User, loggerFactory.CreateLogger(LogCategories.User) },
            { LogEvents.Wand, loggerFactory.CreateLogger(LogCategories.Wand) },
        };
        MagickNET.Log += (_, e) =>
        {
            HandleUsage(e.EventType, e.Message);
            var logLevel = GetEventLogLevel(e.EventType);
            // choose logger
            var logger = loggers.GetValueOrDefault(e.EventType, defaultLogger);
            if (logger.IsEnabled(logLevel))
            {
#pragma warning disable CA2254
                logger.Log(logLevel, e.Message);
#pragma warning restore CA2254
            }
        };
        MagickNET.SetLogEvents(LogEvents.Detailed);
    }

    private void EnableResourceManagedLogging()
    {
        MagickNET.SetLogEvents(LogEvents.Resource);
        MagickNET.Log += (_, e) =>
        {
            HandleUsage(e.EventType, e.Message);
        };
    }

    private void HandleUsage(LogEvents eventType, string? message)
    {
        if (message is not null && eventType == LogEvents.Resource && TryParseSizes(message, out var sizes))
        {
            var (range, _, used, total) = sizes;
            var messageSpan = message.AsSpan();
            var bucketSpan = messageSpan[range];
            if (MemoryExtensions.Equals(bucketSpan, "Area", StringComparison.InvariantCultureIgnoreCase))
            {
                _usage.Area = new BucketUsage { Used = used, Total = total };
            }
            if (MemoryExtensions.Equals(bucketSpan, "Disk", StringComparison.InvariantCultureIgnoreCase))
            {
                _usage.Disk = new BucketUsage { Used = used, Total = total };
            }
            if (MemoryExtensions.Equals(bucketSpan, "File", StringComparison.InvariantCultureIgnoreCase))
            {
                _usage.File = new BucketUsage { Used = used, Total = total };
            }
            if (MemoryExtensions.Equals(bucketSpan, "Map", StringComparison.InvariantCultureIgnoreCase))
            {
                _usage.Map = new BucketUsage { Used = used, Total = total };
            }
            if (MemoryExtensions.Equals(bucketSpan, "Memory", StringComparison.InvariantCultureIgnoreCase))
            {
                _usage.Memory = new BucketUsage { Used = used, Total = total };
            }
        }
    }

    internal ValueTask<Image> FromStreamSyncOrAsync(Stream source, CancellationToken cancellationToken)
    {
        if (Configuration is not null && Configuration.ForceAsync)
        {
            return new(FromStreamAsync(source, cancellationToken));
        }
        return new(FromStream(source));
    }

    /// <summary>
    /// Initializes new instance of <see cref="Image" /> synchronously.
    /// </summary>
    /// <param name="source">Stream containing image data.</param>
    public Image FromStream(Stream source)
    {
        MagickReadSettings settings;
        var configure = Configuration?.ConfigureReadSettings;
        if (configure is null)
        {
            settings = new MagickReadSettings
            {
                Density = new Density(600, 600), // higher PDF quality
                Verbose = false
            };
        }
        else
        {
            settings = new MagickReadSettings();
            configure(settings);
        }
        var collection = new MagickImageCollection(source, settings);
        return new Image(collection, this);
    }

    /// <summary>
    /// Initializes new instance of <see cref="Image" /> asynchronously. The read operation is still synchronous yet
    /// it it forced to be performed as a separate task. This allowes returning immediately which may be important
    /// if otherwise stream production is blocked.
    /// </summary>
    /// <param name="source">Stream containing image data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", MessageId = "cancellationToken")]
    public async Task<Image> FromStreamAsync(Stream source, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return FromStream(source);
    }
}