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
    ///     If this behavior is not desirable, see <see cref="SomeUnsafe{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the contained value/</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A new <see cref="Option{T}" /> containing <paramref name="value" /> on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value" /> is <see langword="null" />.</exception>
    public static Option<T> Some<T>( T value ) => value is null
                                                      ? throw new ArgumentNullException(
                                                            nameof( value ),
                                                            "Some may not store null values"
                                                        )
                                                      : new Option<T>( value );

    /// <summary>
    ///     Constructs an <see cref="Option{T}" /> containing the specified value.
    ///     Does not perform any <see langword="null" /> checks.
    /// </summary>
    /// <typeparam name="T">The type of the contained value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A new <see cref="Option{T}" /> containing <paramref name="value" />.</returns>
    public static Option<T> SomeUnsafe<T>( T value ) => new( value );

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

    public T Expect( string msg ) => this.TryUnwrap( out var x ) ? x : throw new InvalidOperationException( msg );

    public T Unwrap()                   => this.Expect( "cannot unwrap a None value" );
    public T Unwrap( T       fallback ) => this.TryUnwrap( out var value ) ? value : fallback;
    public T Unwrap( Func<T> fallback ) => this.TryUnwrap( out var value ) ? value : fallback();

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

    public Option<T2> Select<T2>( Func<T, T2> selector ) => this.TryUnwrap( out var x ) ? selector( x ) : Option.None;

    public Option<T> Where( Predicate<T> predicate )
        => this.TryUnwrap( out var x ) && predicate( x ) ? this : Option.None;

    public T2 Select<T2>( Func<T, T2> selector, T2 defaultValue )
        => this.TryUnwrap( out var x ) ? selector( x ) : defaultValue;

    public T2 Select<T2>( Func<T, T2> selector, Func<T2> defaultValueFunc )
        => this.TryUnwrap( out var x ) ? selector( x ) : defaultValueFunc();

    public Result<T, TError> OkOr<TError>( TError error )
        => this.TryUnwrap( out var x ) ? Result.Ok( x ) : Result.Err( error );

    public Result<T, TError> OkOr<TError>( Func<TError> errorFunc )
        => this.TryUnwrap( out var x ) ? Result.Ok( x ) : Result.Err( errorFunc() );

    public Option<T2> And<T2>( Option<T2>       other )     => this.IsSome ? other : Option.None;
    public Option<T2> And<T2>( Func<Option<T2>> otherFunc ) => this.IsSome ? otherFunc() : Option.None;

    public Option<T> Or( Option<T>       other )     => this.IsSome ? this : other;
    public Option<T> Or( Func<Option<T>> otherFunc ) => this.IsSome ? this : otherFunc();

    public Option<T> Xor( Option<T> other )
    {
        if( this.IsNone && other.IsSome ) return other;
        if( this.IsSome && other.IsNone ) return this;

        return Option.None;
    }

    public bool Contains( T other ) => this.TryUnwrap( out var x ) && EqualityComparer<T>.Default.Equals( x, other );

    public Option<( T Left, T2 Right )> Zip<T2>( Option<T2> other )
        => this.TryUnwrap( out var left ) && other.TryUnwrap( out var right ) ? ( left, right ) : Option.None;

    public Option<T3> Zip<T2, T3>( Option<T2> other, Func<T, T2, T3> zipFunc )
        => this.TryUnwrap( out var left ) && other.TryUnwrap( out var right ) ? zipFunc( left, right ) : Option.None;

    public void Match( Action<T> some, Action none )
    {
        if( this.TryUnwrap( out var x ) )
        {
            some( x );
            return;
        }

        none();
    }

    public T2 Match<T2>( Func<T, T2> some, Func<T2> none ) => this.TryUnwrap( out var x ) ? some( x ) : none();

    public static explicit operator T( in Option<T> self ) => self.Unwrap();

    public static implicit operator Option<T>( Option.NoneType _ ) => new();

    public static implicit operator Option<T>( T? value )
        => Object.ReferenceEquals( value, null ) ? Option.None : Option.Some( value );
}