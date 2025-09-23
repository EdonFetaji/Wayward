using Microsoft.EntityFrameworkCore;
using Wayward.Domain.DomainModels;
using Wayward.Repository;
using Wayward.Service.Interface;

namespace Wayward.Service.Implementation
{
    public class WishListService : IWishListService
    {
        private readonly IRepository<WishList> _repository;
        private readonly IRepository<Flight> _flightRepository;

        public WishListService(IRepository<WishList> repository, IRepository<Flight> flightRepository)
        {
            _repository = repository;
            _flightRepository = flightRepository;
        }

        public bool DeleteFromWishList(Guid flightId, Guid userId)
        {
            var wishList = GetByUserId(userId);

            if (wishList == null || wishList.Flights == null || wishList.Flights.Count == 0)
                return false;

            if (!wishList.Flights.Any(f => f.Id == flightId))
                return false;

            Flight flight = _flightRepository.Get(selector: x => x, predicate: x => x.Id == flightId);

            wishList.Flights.Remove(flight);


            _repository.Update(wishList);            
            return true;
        }
        public WishList GetByUserId(Guid id)
        {
            return _repository.Get(selector: x => x,
                                    predicate: x => x.OwnerId == id.ToString(),
                                    include: x => x.Include(y => y.Flights));
        }

        public WishList Update(WishList wishlist)
        {
            return _repository.Update(wishlist);
        }
        public WishList Insert(WishList wishlist)
        {
            return _repository.Insert(wishlist);
        }

    }
}
