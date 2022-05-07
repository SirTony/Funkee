using static Funkee.Option;

namespace Funkee;

/// <summary>
///     Utilities for constructing <see cref="Option{T}" /> values.
/// </summary>
public static class Option
{
    /// <summary>
    ///     A useless type meant only to serve as a tool for cleaner usage of <see cref="Option{T}" />
    ///     by implicitly casting to the final type.
    /// </summary>
    public readonly ref struct NoneType { }

    /// <summary>
    ///     Constructs an <see cref="Option{T}" /> containing no value.
    /// </summary>
    public static NoneType None => default;

    /// <summary>
    ///     Constructs an <see cref="Option{T}" /> containing no value of the specified type.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <returns>An <see cref="Option{T}" /> containing no value.</returns>
    public static Option<T> NoneOf<T>() => new();

    /// <summary>
    ///     Constructs an <see cref="Option{T}" /> containing the specified value.
    ///     This method disallows <see langword="null" /> values.
    /// </summary>
    /// <typeparam name="T">The type of the contained value/</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A new <see cref="Option{T}" /> containing <paramref name="value" /> on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value" /> is <see langword="null" />.</exception>
    public static Option<T> Some<T>( T value ) => value is not null
                                                      ? new Option<T>( value )
                                                      : throw new ArgumentNullException(
                                                            nameof( value ),
                                                            "Some may not store null values"
                                                        );

    /// <summary>
    ///     Converts a <seealso cref="Nullable{T}" /> to an <see cref="Option{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the contained value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    ///     If <paramref name="value" /> is <see langword="null" /> then <see cref="None" /> is returned, otherwise
    ///     <see cref="Some{T}" /> will be returned.
    /// </returns>
    public static Option<T> From<T>( T? value ) where T : struct
        => value.HasValue ? Option.Some( value.Value ) : Option.NoneOf<T>();

    /// <summary>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="zippedOption"></param>
    /// <returns></returns>
    public static ( Option<T1>, Option<T2> ) Unzip<T1, T2>( Option<( T1, T2 )> zippedOption )
    {
        if( zippedOption.IsNone ) return ( Option.NoneOf<T1>(), Option.NoneOf<T2>() );

        var (x, y) = zippedOption.Unwrap();
        return ( Option.Some( x ), Option.Some( y ) );
    }
}

/// <summary>
///     An immutable value type representing a value that may or may not exist.
///     Intended as a safer alternative to <seealso langword="null" />.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public readonly struct Option<T>
{
    /// <summary>
    ///     <see langword="true" /> if this instance contains a value, otherwise <see langword="false" />.
    /// </summary>
    public bool IsSome { get; }

    /// <summary>
    ///     <see langword="true" /> if this instance does not contain a value, otherwise <see langword="false" />.
    /// </summary>
    public bool IsNone => !this.IsSome;

    private readonly T _value = default!;

    /// <summary>
    ///     Constructs a new none value. Not intended to be used directly. See <see cref="Option.None" /> or
    ///     <see cref="Option.NoneOf{T}" /> instead.
    /// </summary>
    public Option() => this.IsSome = false;

    internal Option( T value )
    {
        this._value = value;
        this.IsSome = true;
    }

    /// <summary>
    ///     Checks if the contained value (if present) matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The condition to apply the contained value (if present) to.</param>
    /// <returns>
    ///     <see langword="true" /> if a value is present and <paramref name="predicate" /> returns
    ///     <see langword="true" />, otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate" /> is <see langword="null" />.</exception>
    public bool IsSomeBy( Predicate<T> predicate ) => predicate is not null
                                                          ? this.TryUnwrap( out var x ) && predicate( x )
                                                          : throw new ArgumentNullException( nameof( predicate ) );

    /// <summary>
    ///     Attempts to unwrap a Some value and throws an exception with a specified message on error.
    /// </summary>
    /// <param name="msg">The exception message on failure.</param>
    /// <returns>The wrapped value if present.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to unwrap a None.</exception>
    public T Expect( string msg ) => this.TryUnwrap( out var x ) ? x : throw new InvalidOperationException( msg );

    /// <summary>
    ///     Attempts to unwrap a Some value and throws an exception on error.
    /// </summary>
    /// <returns>The wrapped value if present.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to unwrap a None.</exception>
    public T Unwrap() => this.Expect( "cannot unwrap a None value" );

    /// <summary>
    ///     Attempts to unwrap a Some value.
    /// </summary>
    /// <param name="fallback">The default value to use if the option is None.</param>
    /// <returns>The wrapped value if present, otherwise <paramref name="fallback" />.</returns>
    public T Unwrap( T fallback ) => this.TryUnwrap( out var value ) ? value : fallback;

    /// <summary>
    ///     Attempts to unwrap a Some value.
    /// </summary>
    /// <param name="fallback"></param>
    /// <returns></returns>
    public T Unwrap( Func<T> fallback ) => this.TryUnwrap( out var value ) ? value : fallback();

    /// <summary>
    ///     Attempts to unwrap a Some value.
    /// </summary>
    /// <param name="value">The wrapped value, if present.</param>
    /// <returns><see langword="true" /> on success, <see langword="false" /> otherwise.</returns>
    public bool TryUnwrap( out T value )
    {
        if( this.IsNone )
        {
            value = default!;
            return false;
        }

        value = this._value;
        return true;
    }

    /// <summary>
    ///     Applies a mapping function to the wrapped value, if present.
    /// </summary>
    /// <typeparam name="T2">The type of the mapped value.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <returns>A Some containing the new value if the original value was Some, otherwise None.</returns>
    public Option<T2> Select<T2>( Func<T, T2> selector ) => this.TryUnwrap( out var x ) ? selector( x ) : Option.None;

    /// <summary>
    ///     Filters a Some value through the specified <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">The condition to apply to a wrapped value, if present.</param>
    /// <returns>A Some value if <paramref name="predicate" /> returns <see langword="true" />, otherwise None.</returns>
    public Option<T> Where( Predicate<T> predicate )
        => this.TryUnwrap( out var x ) && predicate( x ) ? this : Option.None;

    /// <summary>
    ///     Applies a mapping function to the wrapped value, if present.
    /// </summary>
    /// <typeparam name="T2">The type of the mapped value.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <param name="defaultValue">The value to return if the Option is None.</param>
    /// <returns>The mapped value if the Option was Some, otherwise <paramref name="defaultValue" />.</returns>
    public T2 Select<T2>( Func<T, T2> selector, T2 defaultValue )
        => this.TryUnwrap( out var x ) ? selector( x ) : defaultValue;

    /// <summary>
    ///     Applies a mapping function to the wrapped value, if present.
    /// </summary>
    /// <typeparam name="T2"> The type of the mapped value.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <param name="defaultValueFunc">A factory function returning a default value if the Option is None.</param>
    /// <returns>The mapped value if the Option was Some, otherwise the return value of <paramref name="defaultValueFunc" />.</returns>
    public T2 Select<T2>( Func<T, T2> selector, Func<T2> defaultValueFunc )
        => this.TryUnwrap( out var x ) ? selector( x ) : defaultValueFunc();

    /// <summary>
    ///     Transforms the Option value into a <see cref="Result{TValue,TError}" />.
    /// </summary>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="error">The error value if the Option is None.</param>
    /// <returns>An Ok containing the Some value, or an Err containing <paramref name="error" /> if the Option was None.</returns>
    public Result<T, TError> OkOr<TError>( TError error )
        => this.TryUnwrap( out var x ) ? Result.Ok( x ) : Result.Err( error );

    /// <summary>
    ///     Transforms the Option value into a <see cref="Result{TValue,TError}" />.
    /// </summary>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="errorFunc">A factory function generating an error value if the Option is None.</param>
    /// <returns>
    ///     An Ok containing the Some value, or an Err containing the return value of <paramref name="errorFunc" /> if the
    ///     Option was None.
    /// </returns>
    public Result<T, TError> OkOr<TError>( Func<TError> errorFunc )
        => this.TryUnwrap( out var x ) ? Result.Ok( x ) : Result.Err( errorFunc() );

    /// <summary>
    ///     Tests that both Option values are a Some value using boolean AND logic.
    /// </summary>
    /// <typeparam name="T2">The type of the second Option's value.</typeparam>
    /// <param name="other">The right-hand Option.</param>
    /// <returns><paramref name="other" /> if <see langword="this" /> is Some, otherwise None.</returns>
    public Option<T2> And<T2>( Option<T2> other ) => this.IsSome ? other : Option.None;

    /// <summary>
    ///     Tests that both Option values are a Some value using boolean AND logic.
    /// </summary>
    /// <typeparam name="T2">The type of the second Option's value.</typeparam>
    /// <param name="otherFunc">A factory function generating the right-hand Option.</param>
    /// <returns>The return value of <paramref name="otherFunc" /> if <see langword="this" /> is Some, otherwise None.</returns>
    public Option<T2> And<T2>( Func<Option<T2>> otherFunc ) => this.IsSome ? otherFunc() : Option.None;

    /// <summary>
    ///     Tests that either Option values are Some using boolean OR logic.
    /// </summary>
    /// <param name="other">The right-hand Option.</param>
    /// <returns><see langword="this" /> if <see langword="this" /> is Some, otherwise <paramref name="other" />.</returns>
    public Option<T> Or( Option<T> other ) => this.IsSome ? this : other;

    /// <summary>
    ///     Tests that either Option values are Some using boolean OR logic.
    /// </summary>
    /// <param name="otherFunc">A factory function generating the right-hand Option.</param>
    /// <returns>
    ///     <see langword="this" /> if <see langword="this" /> is Some, otherwise the return value of
    ///     <paramref name="otherFunc" />.
    /// </returns>
    public Option<T> Or( Func<Option<T>> otherFunc ) => this.IsSome ? this : otherFunc();

    /// <summary>
    ///     Applies exclusive-OR logic to two Option values, ensuring that one (but not both) is Some.
    /// </summary>
    /// <param name="other">The right-hand Option.</param>
    /// <returns>
    ///     <see langword="this" /> if <see langword="this" /> is Some and <paramref name="other" /> is None.
    ///     <paramref name="other" /> if <paramref name="other" /> is Some and <see langword="this" /> is None.
    ///     Otherwise None.
    /// </returns>
    public Option<T> Xor( Option<T> other )
    {
        if( this.IsSome && other.IsNone ) return this;
        if( this.IsNone && other.IsSome ) return other;

        return Option.None;
    }

    /// <summary>
    ///     Applies exclusive-OR logic to two Option values, ensuring that one (but not both) is Some.
    /// </summary>
    /// <param name="otherFunc">A factory function returning the right-hand Option.</param>
    /// <returns>
    ///     <see langword="this" /> if <see langword="this" /> is Some and the result of <paramref name="otherFunc" /> is None.
    ///     <paramref name="otherFunc" /> if <paramref name="otherFunc" /> is Some and <see langword="this" /> is None.
    ///     Otherwise None.
    /// </returns>
    public Option<T> Xor( Func<Option<T>> otherFunc )
    {
        var other = otherFunc();

        if( this.IsSome && other.IsNone ) return this;
        if( this.IsNone && other.IsSome ) return other;

        return Option.None;
    }

    /// <summary>
    ///     Tests if the Option contains the specified value.
    /// </summary>
    /// <param name="value">The value to check for.</param>
    /// <param name="eq">An optional <see cref="IEqualityComparer{T}" /> for testing the values.</param>
    /// <returns>
    ///     <see langword="true" /> if the Option is Some and contains the specified value, <see langword="false" />
    ///     otherwise.
    /// </returns>
    public bool Contains( T value, IEqualityComparer<T>? eq = null )
        => this.TryUnwrap( out var x ) && ( eq ?? EqualityComparer<T>.Default ).Equals( x, value );

    /// <summary>
    ///     Tests if the Option contains the specified value.
    /// </summary>
    /// <param name="valueFunc">A factory function that returns a value to check for.</param>
    /// <param name="eq">An optional <see cref="IEqualityComparer{T}" /> for testing the values.</param>
    /// <returns>
    ///     <see langword="true" /> if the Option is Some and contains the specified value, <see langword="false" />
    ///     otherwise.
    /// </returns>
    public bool Contains( Func<T> valueFunc, IEqualityComparer<T>? eq = null )
        => this.TryUnwrap( out var x ) && ( eq ?? EqualityComparer<T>.Default ).Equals( x, valueFunc() );

    /// <summary>
    ///     Zips two separate <seealso cref="Option{T}" /> instances together into one that contains a tuple of both values.
    /// </summary>
    /// <typeparam name="T2">The type of the second Option's value.</typeparam>
    /// <param name="other">The second Option.</param>
    /// <returns>A Some containing a tuple of the separate Option's values if both options were Some, otherwise None.</returns>
    public Option<( T Left, T2 Right )> Zip<T2>( Option<T2> other )
        => this.TryUnwrap( out var left ) && other.TryUnwrap( out var right ) ? ( left, right ) : Option.None;

    /// <summary>
    ///     Zips two separate <seealso cref="Option{T}" /> instances together into one that contains the result of a zip
    ///     function.
    /// </summary>
    /// <typeparam name="T2">The type of the second Option's value.</typeparam>
    /// <typeparam name="T3">The zip result type.</typeparam>
    /// <param name="other">The second Option.</param>
    /// <param name="zipFunc">The function that combines both values.</param>
    /// <returns>A Some containing the result of <paramref name="zipFunc" /> if both options were Some, otherwise None.</returns>
    public Option<T3> Zip<T2, T3>( Option<T2> other, Func<T, T2, T3> zipFunc )
        => this.TryUnwrap( out var left ) && other.TryUnwrap( out var right ) ? zipFunc( left, right ) : Option.None;

    /// <summary>
    ///     Serves as a stand-in for match or switch statements. Does not return a value.
    /// </summary>
    /// <param name="some">Invoked when the Option is Some.</param>
    /// <param name="none">Invoked when the Option is None.</param>
    public void Match( Action<T> some, Action none )
    {
        if( this.TryUnwrap( out var x ) )
        {
            some( x );
            return;
        }

        none();
    }

    /// <summary>
    ///     Serves as a stand-in for match or switch expressions by returning values for both Some and None cases.
    /// </summary>
    /// <typeparam name="T2">The result type.</typeparam>
    /// <param name="some">Invoked when the Option is Some.</param>
    /// <param name="none">Invoked when the Option is None.</param>
    /// <returns>
    ///     The result of either <paramref name="some" /> or <paramref name="none" /> depending on the state of the
    ///     Option.
    /// </returns>
    public T2 Match<T2>( Func<T, T2> some, Func<T2> none ) => this.TryUnwrap( out var x ) ? some( x ) : none();

    /// <summary>
    ///     Attempts force-unwrap the Option. Throws if the Option is None.
    /// </summary>
    /// <param name="self">The option to unwrap.</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to unwrap a None.</exception>
    public static explicit operator T( in Option<T> self ) => self.Unwrap();

    /// <summary>
    ///     Utility operator to seamlessly turn <see cref="NoneType" /> into an appropriately typed <see cref="Option{T}" />.
    /// </summary>
    /// <param name="_">Dummy value, not used.</param>
    public static implicit operator Option<T>( NoneType _ ) => new();

    /// <summary>
    ///     Utility operator to seamlessly turn values of <typeparamref name="T" /> into <see cref="Option{T}" />.
    ///     Returns a None value if <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Option<T>( T? value )
        => Object.ReferenceEquals( value, null ) ? Option.None : Option.Some( value );
}