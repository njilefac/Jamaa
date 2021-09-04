using System;
using Domain.Entities.Shared;

namespace Domain.Entities.Members
{
    public class ContactDetails
    {
        public ContactDetails(Guid id, string email, string phoneNumber, Address address)
        {
            Id = id;
            Email = email;
            PhoneNumber = phoneNumber;
            Address = address;
        }
        
        private ContactDetails()
        :this(Guid.Empty, string.Empty, String.Empty, Address.None)
        {
        }

        public static ContactDetails None { get; } = new ContactDetails();

        public Guid Id { get;  }
        public string Email { get;  }
        public string PhoneNumber { get;  }
        public Address Address { get;  }
    }
}