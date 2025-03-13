namespace WinsorApps.Services.Global;

public sealed record Optional<T> where T : class
{
    private T? _object = null;

    public static Optional<T> Some(T? obj) => new() { _object = obj };
    public static Optional<T> None() => new();

    public Optional<TResult> Map<TResult>(Func<T, TResult> map) where TResult : class =>
        _object is null ? Optional<TResult>.None() : Optional<TResult>.Some(map(_object));
    public OptionalStruct<TResult> MapStruct<TResult>(Func<T, TResult> map) where TResult : struct =>
        _object is null ? OptionalStruct<TResult>.None() : OptionalStruct<TResult>.Some(map(_object));

    public T Reduce(T @default) => _object ?? @default;
}

public struct OptionalStruct<T> where T : struct
{
    private T? _value = null;

    public OptionalStruct() { }

    public static OptionalStruct<T> Some(T value) => new() { _value = value };
    public static OptionalStruct<T> None() => new();
    public readonly OptionalStruct<TResult> Map<TResult>(Func<T, TResult> map) where TResult : struct =>
        _value is not null ? OptionalStruct<TResult>.Some(map(_value.Value)) : OptionalStruct<TResult>.None();
    public readonly Optional<TResult> MapObject<TResult>(Func<T, TResult> map) where TResult : class =>
        _value is not null ? Optional<TResult>.Some(map(_value.Value)) : Optional<TResult>.None();

    public readonly TResult Map<TResult>(Func<T, TResult> map, TResult defaultValue) where TResult : class =>
        _value is not null ? map(_value.Value) : defaultValue;

    public readonly T Reduce(T @default) => _value ?? @default;
}

public static class MonadExtensions
{
    public static Optional<T> FirstOrNone<T>(this IEnumerable<T> values, Func<T, bool> predicate) where T : class
    {
        var result = values.FirstOrDefault(predicate);
        return result is null ? Optional<T>.None() : Optional<T>.Some(result);
    }

    public static OptionalStruct<T> FirstStructOrNone<T>(this IEnumerable<T> values, Func<T, bool> predicate) where T : struct
    {
        return values.Any(predicate) ? OptionalStruct<T>.None() : OptionalStruct<T>.Some(values.First(predicate));
    }
}