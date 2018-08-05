using System;
using System.Linq;
using System.Linq.Expressions;

namespace Dapperer
{
    public abstract class EntityLoader<TEntity, TPrimaryKey>
        where TEntity : class, IIdentifier<TPrimaryKey>, new()
    {
        protected static Action<TEntity, TReferenceEntity> GetSetter<TReferenceEntity>(Expression<Func<TEntity, TReferenceEntity>> foreignEntity)
            where TReferenceEntity : class
        {
            var valueParameterExpression = Expression.Parameter(typeof(TReferenceEntity));
            var targetExpression = foreignEntity.Body is UnaryExpression ? ((UnaryExpression)foreignEntity.Body).Operand : foreignEntity.Body;

            var assign = Expression.Lambda<Action<TEntity, TReferenceEntity>>(
                Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                foreignEntity.Parameters.Single(),
                valueParameterExpression);

            return assign.Compile();
        }
    }
}
