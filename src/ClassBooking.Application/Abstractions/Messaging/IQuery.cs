using ClassBooking.Domain.Common;
using MediatR;

namespace ClassBooking.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
