namespace Funkee;

/// <summary>
///     A utility class meant to assist with constructing <see cref="Result{TValue,TError}" />.
/// </summary>
public static class Result
{
    /// <summary>
    ///     An intermediate wrapper type containing a value indicating a success result.
    ///     Not meant to be used directly.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public readonly ref struct OkValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    Value { get; }

        /// <summary>
        ///     Required by the framework, but invokation this constructor is not intended and will throw an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        [Obsolete( "This constructor is not meant to invoked." )]
        public OkValue() => throw new InvalidOperationException();

        internal OkValue( T value )
        {
            this.IsDefault = false;
            this.Value     = value;
        }
    }

    /// <summary>
    ///     An intermediate wrapper type containing a value indicating an error result.
    ///     Not meant to be used directly.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped error.</typeparam>
    public readonly ref struct ErrValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    Error { get; }

        /// <summary>
        ///     Required by the framework, but invokation this constructor is not intended and will throw an exception.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown.</exception>
        [Obsolete( "This constructor is not meant to invoked." )]
        public ErrValue() => throw new InvalidOperationException();

        internal ErrValue( T error )
        {
            this.IsDefault = false;
            this.Error     = error;
        }
    }

    /// <summary>
    ///     Constructs a new success result containing the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A success result containing the specified value.</returns>
    public static OkValue<T> Ok<T>( T value ) => new( value );

    /// <summary>
    ///     Constructs a new error result containing the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped error.</typeparam>
    /// <param name="error">The error value.</param>
    /// <returns>An error result containing the specified value.</returns>
    public static ErrValue<T> Err<T>( T error ) => new( error );
}

/// <summary>
///     A type containing one of two possible values, Ok on success, or Err on error.
/// </summary>
/// <typeparam name="TValue">The Ok value type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public abstract record class Result<TValue, TError>
{
    private sealed record class Ok( TValue Value ) : Result<TValue, TError>( true );

    private sealed record class Err( TError Error ) : Result<TValue, TError>( false );

    /// <summary>
    ///     <see langword="true" /> if the result indicates success, <see langword="false" /> otherwise.
    /// </summary>
    public bool IsOk { get; }

    /// <summary>
    ///     <see langword="true" /> if the result indicates an error, <see langword="false" /> otherwise.
    /// </summary>
    public bool IsError => !this.IsOk;

    private Result() => throw new InvalidOperationException();
    private Result( bool isOk ) => this.IsOk = isOk;

    /// <summary>
    ///     Determines if the current result indicates success and satisfies a condition.
    /// </summary>
    /// <param name="predicate">The condition to test against.</param>
    /// <returns><see langword="true" /> if the result contains a value matching <paramref name="predicate" />.</returns>
    public bool IsOkBy( Predicate<TValue> predicate ) => this switch
    {
        Ok { Value: var x } => predicate( x ),
        _                   => false,
    };

    /// <summary>
    ///     Determines if the current result indicates an error and satisfies a condition.
    /// </summary>
    /// <param name="predicate">The condition to test against.</param>
    /// <returns><see langword="true" /> if the result contains an error matching <paramref name="predicate" />.</returns>
    public bool IsErrBy( Predicate<TError> predicate ) => this switch
    {
        Err { Error: var e } => predicate( e ),
        _                    => false,
    };

    /// <summary>
    ///     Attempts to unwrap the success value.
    /// </summary>
    /// <returns>The wrapped value if the result indicates success.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result contains an error value.</exception>
    public TValue Unwrap() => this switch
    {
        Ok { Value: var x } => x,
        _                   => throw new InvalidOperationException( "cannot unwrap an error value" ),
    };

    /// <summary>
    ///     Attempts to unwrap the error value.
    /// </summary>
    /// <returns>The wrapped error if the result indicates error.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result indicates success.</exception>
    public TError UnwrapErr() => this switch
    {
        Err { Error: var e } => e,
        _                    => throw new InvalidOperationException( "cannot unwrap an ok value" ),
    };

    /// <summary>
    ///     Attempts to unwrap a success result.
    /// </summary>
    /// <param name="value">The default value to return on error.</param>
    /// <returns>The wrapped value if the result indicates success, otherwise <paramref name="value" />.</returns>
    public TValue UnwrapOr( TValue value ) => this switch
    {
        Ok { Value: var x } => x,
        _                   => value,
    };

    /// <summary>
    ///     Attempts to unwrap an error result.
    /// </summary>
    /// <param name="error">The default error to return is the result is Ok.</param>
    /// <returns>The wrapped error if the result contains one, otherwise <paramref name="error" />.</returns>
    public TError UnwrapErrOr( TError error ) => this switch
    {
        Err { Error: var e } => e,
        _                    => error,
    };

    public TValue UnwrapOr( Func<TValue> valueFunc ) => this switch
    {
        Ok { Value: var x } => x,
        _                   => valueFunc(),
    };

    public TError UnwrapErrOr( Func<TError> errorFunc ) => this switch
    {
        Err { Error: var e } => e,
        _                    => errorFunc(),
    };

    public bool TryUnwrap( out TValue value )
    {
        switch( this )
        {
            case Ok { Value: var x }:
                value = x;
                return true;

            default:
                value = default!;
                return false;
        }
    }

    public bool TryUnwrapErr( out TError error )
    {
        switch( this )
        {
            case Err { Error: var e }:
                error = e;
                return true;

            default:
                error = default!;
                return true;
        }
    }

    public bool Contains( TValue value )
        => this.TryUnwrap( out var x ) && EqualityComparer<TValue>.Default.Equals( x, value );

    public bool ContainsErr( TError error )
        => this.TryUnwrapErr( out var e ) && EqualityComparer<TError>.Default.Equals( e, error );

    public Result<TValue, TError> Filter( Predicate<TValue> condition, Func<TValue, TError> orElse ) => this switch
    {
        Ok { Value: var x } when condition( x ) => this,
        Ok { Value: var x }                     => new Err( orElse( x ) ),
        Err _                                   => this,
        _                                       => throw new NotImplementedException(),
    };

    public Result<TValue2, TError> Select<TValue2>( Func<TValue, Result<TValue2, TError>> selector ) => this switch
    {
        Ok { Value : var x } => selector( x ),
        Err { Error: var e } => e,
        _                    => throw new NotImplementedException(),
    };

    public Result<TValue, TError2> SelectErr<TError2>( Func<TError, Result<TValue, TError2>> selector ) => this switch
    {
        Ok { Value : var x } => x,
        Err { Error: var e } => selector( e ),
        _                    => throw new NotImplementedException(),
    };

    public TValue2 Select<TValue2>( Func<TValue, TValue2> selector, TValue2 defaultValue ) => this switch
    {
        Ok { Value: var x } => selector( x ),
        _                   => defaultValue,
    };

    public TError2 SelectErr<TError2>( Func<TError, TError2> selector, TError2 defaultValue ) => this switch
    {
        Err { Error: var x } => selector( x ),
        _                    => defaultValue,
    };

    public TValue2 Select<TValue2>( Func<TValue, TValue2> selector, Func<TValue2> defaultValueFunc ) => this switch
    {
        Ok { Value: var x } => selector( x ),
        _                   => defaultValueFunc(),
    };

    public Result<TValue, TError2> SelectErr<TError2>( Func<TError, TError2> selector, Func<TError2> defaultErrorFunc )
        => this switch
        {
            Err { Error: var e } => selector( e ),
            _                    => defaultErrorFunc(),
        };

    public TValue Expect( string msg ) => this.TryUnwrap( out var x ) ? x : throw new InvalidCastException( msg );

    public TError ExpectErr( string msg ) => this.TryUnwrapErr( out var e ) ? e : throw new InvalidCastException( msg );

    public Result<TValue2, TError> And<TValue2>( Result<TValue2, TError> other ) => this switch
    {
        Ok _                 => other,
        Err { Error: var e } => e,
        _                    => throw new NotImplementedException(),
    };

    public Result<TValue2, TError> And<TValue2>( Func<Result<TValue2, TError>> otherFunc ) => this switch
    {
        Ok _                 => otherFunc(),
        Err { Error: var e } => e,
        _                    => throw new NotImplementedException(),
    };

    public Result<TValue, TError2> Or<TError2>( Result<TValue, TError2> other ) => this switch
    {
        Ok { Value: var x } => x,
        Err _               => other,
        _                   => throw new NotImplementedException(),
    };

    public Result<TValue, TError2> Or<TError2>( Func<Result<TValue, TError2>> otherFunc ) => this switch
    {
        Ok { Value: var x } => x,
        Err _               => otherFunc(),
        _                   => throw new NotImplementedException(),
    };

    public void Match( Action<TValue> ok, Action<TError> err )
    {
        switch( this )
        {
            case Ok { Value: var v }:
                ok( v );
                break;

            case Err { Error: var e }:
                err( e );
                break;
        }
    }

    public TValue2 Match<TValue2>( Func<TValue, TValue2> ok, Func<TError, TValue2> err ) => this switch
    {
        Ok { Value : var x } => ok( x ),
        Err { Error: var e } => err( e ),
        _                    => throw new NotImplementedException(),
    };

    public static explicit operator Option<TValue>( Result<TValue, TError> self )
        => self.TryUnwrap( out var x ) ? Option.Some( x ) : Option.None;

    public static implicit operator Result<TValue, TError>( in Result.OkValue<TValue>  ok )  => new Ok( ok.Value );
    public static implicit operator Result<TValue, TError>( in Result.ErrValue<TError> err ) => new Err( err.Error );

    public static implicit operator Result<TValue, TError>( TValue value ) => new Ok( value );
    public static implicit operator Result<TValue, TError>( TError error ) => new Err( error );
}