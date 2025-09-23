using Wayward.Domain.DomainModels;
using Wayward.Domain.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wayward.Service.Interface
{
    public interface IWishListService
    {
        WishList GetByUserId(Guid id);
        WishList Update(WishList wishlist);
        Boolean DeleteFromWishList(Guid id, Guid userId);
        WishList Insert(WishList wishlist);
    }
}
