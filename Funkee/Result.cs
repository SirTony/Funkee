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
        ///     Required by the framework, but invocation this constructor is not intended and will throw an exception.
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
        ///     Required by the framework, but invocation this constructor is not intended and will throw an exception.
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
public abstract record Result<TValue, TError>
{
    /// <summary>
    ///     Represents a successful result.
    /// </summary>
    /// <param name="Value">The wrapped value.</param>
    public sealed record Ok( TValue Value ) : Result<TValue, TError>( true );

    /// <summary>
    ///     Represents an error state.
    /// </summary>
    /// <param name="Error">The wrapped error.</param>
    public sealed record Err( TError Error ) : Result<TValue, TError>( false );

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
    ///     LINQ query syntax support. Not intended to be invoked directly.
    /// </summary>
    /// <typeparam name="TValue2">The type of the mapped value.</typeparam>
    /// <param name="selector">The mapping function.</param>
    /// <returns>A new <see cref="Result{TValue,TError}" /> containing either the mapped value or the wrapped error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selector" /> is <see langword="null" />.</exception>
    public Result<TValue2, TError> Select<TValue2>( Func<TValue, TValue2> selector )
    {
        if( selector is null ) throw new ArgumentNullException( nameof( selector ) );

        return this switch
        {
            Ok(var x)  => selector( x ),
            Err(var e) => e,
            _          => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///     LINQ query syntax support. Not intended to be invoked directly.
    /// </summary>
    /// <typeparam name="TValue2">The type of the intermediate mapped value.</typeparam>
    /// <typeparam name="TValue3">The type of the final mapped value.</typeparam>
    /// <param name="selector">The intermediate mapping function.</param>
    /// <param name="projector">The final mapping function.</param>
    /// <returns>A new <see cref="Result{TValue,TError}" /> containing either the mapped value or the wrapped error.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="selector" /> or <paramref name="projector" /> are
    ///     <see langword="null" />.
    /// </exception>
    public Result<TValue3, TError> SelectMany<TValue2, TValue3>(
        Func<TValue, Result<TValue2, TError>> selector,
        Func<TValue, TValue2, TValue3>        projector
    )
    {
        if( selector is null ) throw new ArgumentNullException( nameof( selector ) );
        if( projector is null ) throw new ArgumentNullException( nameof( projector ) );

        return this switch
        {
            Ok(var x) => selector( x ) switch
            {
                Result<TValue2, TError>.Ok(var y)  => new Result<TValue3, TError>.Ok( projector( x, y ) ),
                Result<TValue2, TError>.Err(var e) => e,
                _                                  => throw new NotImplementedException(),
            },
            Err(var e) => e,
            _          => throw new NotImplementedException(),
        };
    }

    /// <summary>
    ///     Convert a <see cref="Result{TValue,TError}" /> to <see cref="Option{T}" />.
    ///     Ok values become Some, maintaining the value and Err values become None, discarding the error.
    /// </summary>
    /// <param name="self">The <see cref="Result{TValue,TError}" /> to convert.</param>
    public static explicit operator Option<TValue>( Result<TValue, TError> self )
        => self is Ok(var x) ? Option.Some( x ) : Option.None;

    /// <summary>
    ///     Attempts to unwrap an Ok value.
    /// </summary>
    /// <param name="self">The <see cref="Result{TValue,TError}" /> to convert.</param>
    public static explicit operator TValue( Result<TValue, TError> self )
        => self is Ok(var x) ? x : throw new InvalidCastException();

    /// <summary>
    ///     Attempts to unwrap an Err value.
    /// </summary>
    /// <param name="self">The <see cref="Result{TValue,TError}" /> to convert.</param>
    public static explicit operator TError( Result<TValue, TError> self )
        => self is Err(var e) ? e : throw new InvalidCastException();

    /// <summary>
    ///     A helper operator to make constructing results more seamless.
    /// </summary>
    /// <param name="ok">The intermediate Ok value.</param>
    public static implicit operator Result<TValue, TError>( in Result.OkValue<TValue> ok ) => new Ok( ok.Value );

    /// <summary>
    ///     A helper operator to make constructing results more seamless.
    /// </summary>
    /// <param name="err">The intermediate Err value.</param>
    public static implicit operator Result<TValue, TError>( in Result.ErrValue<TError> err ) => new Err( err.Error );

    /// <summary>
    ///     A helper operator to make constructing results more seamless.
    /// </summary>
    /// <param name="value">The Ok value to wrap.</param>
    public static implicit operator Result<TValue, TError>( TValue value ) => new Ok( value );

    /// <summary>
    ///     A helper operator to make constructing results more seamless.
    /// </summary>
    /// <param name="error">The Err value to wrap.</param>
    public static implicit operator Result<TValue, TError>( TError error ) => new Err( error );
}