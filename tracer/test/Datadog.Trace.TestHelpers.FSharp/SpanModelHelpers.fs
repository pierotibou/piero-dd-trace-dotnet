namespace Datadog.Trace.TestHelpers.FSharp

module SpanModelHelpers =
    open ValidationTypes
    open Datadog.Trace.TestHelpers

    // Extraction functions that take a span and return a tuple:
    // (propertyName, value)
    let traceId (span: MockTracerAgent.Span) =
        ( (nameof span.TraceId), span.TraceId.ToString() )

    let spanId (span: MockTracerAgent.Span) =
        ( (nameof span.SpanId), span.SpanId.ToString() )

    let resource (span: MockTracerAgent.Span) =
        ( (nameof span.Resource), span.Resource )
    
    let name (span: MockTracerAgent.Span) =
        ( (nameof span.Name), span.Name )

    let service (span: MockTracerAgent.Span) =
        ( (nameof span.Service), span.Service )

    let ``type`` (span: MockTracerAgent.Span) =
        ( (nameof span.Type), span.Type )

    let tagIsPresent tagName (span: MockTracerAgent.Span) =
        match span.GetTag tagName with
        | null -> Failure $"Tag {tagName} is missing"
        | _ -> Success span

    let tagIsOptional tagName (span: MockTracerAgent.Span) =
        match span.GetTag tagName with
        | _ -> Success span
            
    let tagMatches tagName expectedValue (span: MockTracerAgent.Span) =
        match span.GetTag tagName with
        | null -> Failure $"Tag {tagName} is missing"
        | value when value <> expectedValue -> Failure $"Tag {tagName} value {value} does not match {expectedValue}"
        | _ -> Success span

    let metricIsPresent metricName (span: MockTracerAgent.Span) =
        match span.GetMetric metricName with
        | metric when not metric.HasValue -> Failure $"Tag {metricName} is missing"
        | _ -> Success span

    let metricMatches metricName expectedValue (span: MockTracerAgent.Span) =
        match span.GetMetric metricName with
        | metric when not metric.HasValue -> Failure $"Metric {metricName} is missing"
        | metric when metric.Value <> expectedValue -> Failure $"Metric {metricName} value {metric.Value} does not match {expectedValue}"
        | _ -> Success span

    let isPresentAndNonZero extractProperty (span: MockTracerAgent.Span) =
        match extractProperty span with
        | (propertyName, "") -> Failure $"{propertyName} is empty"
        | (propertyName, null) -> Failure $"{propertyName} is null"
        | (propertyName, "0") -> Failure $"{propertyName} is 0"
        | (_, _) -> Success span
    
    let isPresent extractProperty (span: MockTracerAgent.Span) =
        match extractProperty span with
        | (propertyName, "") -> Failure $"{propertyName} is empty"
        | (propertyName, null) -> Failure $"{propertyName} is null"
        | (_, _) -> Success span

    let matches extractProperty (expectedValue: string) (span:MockTracerAgent.Span) =
        match extractProperty span with
        | (propertyName, value) when value <> expectedValue -> Failure $"{propertyName} value {value} does not match {expectedValue}"
        | (_, _) -> Success span