using ClassBooking.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClassBooking.Application.Abstractions.Data;

public interface IAppDbContext
{
  DbSet<User> Users { get; }
}
