namespace Funkee;

public static class Either
{
    public readonly ref struct LeftValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    Value { get; }

        public LeftValue()
        {
            this.IsDefault = true;
            this.Value     = default!;
        }

        internal LeftValue( T value )
        {
            this.IsDefault = false;
            this.Value     = value;
        }
    }

    public readonly ref struct RightValue<T>
    {
        internal readonly bool IsDefault;
        internal          T    Value { get; }

        public RightValue()
        {
            this.IsDefault = true;
            this.Value     = default!;
        }

        internal RightValue( T value )
        {
            this.IsDefault = false;
            this.Value     = value;
        }
    }

    public static LeftValue<T>  Left<T>( T  value ) => new( value );
    public static RightValue<T> Right<T>( T right ) => new( right );
}

public abstract record Either<TLeft, TRight>
{
    public sealed record Left( TLeft Value ) : Either<TLeft, TRight>( true );

    public sealed record Right( TRight Value ) : Either<TLeft, TRight>( false );

    public bool IsLeft  { get; }
    public bool IsRight => !this.IsLeft;

    private Either() => throw new InvalidOperationException();
    private Either( bool isLeft ) => this.IsLeft = isLeft;

    public static implicit operator Either<TLeft, TRight>( in Either.LeftValue<TLeft> left ) => new Left( left.Value );

    public static implicit operator Either<TLeft, TRight>( in Either.RightValue<TRight> right )
        => new Right( right.Value );

    public static implicit operator Either<TLeft, TRight>( TLeft  value ) => new Left( value );
    public static implicit operator Either<TLeft, TRight>( TRight right ) => new Right( right );
}