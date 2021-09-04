using System;

namespace Domain.Entities.Shared
{
    public class Address
    {
        public Address(Guid id, string city, string street, string houseNumber, string postalCode, string country)
        {
            Id = id;
            City = city;
            Street = street;
            HouseNumber = houseNumber;
            PostalCode = postalCode;
            Country = country;
        }

        private Address()
            : this(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        {
        }

        public Guid Id { get; }
        public string City { get; }
        public string Street { get; }
        public string HouseNumber { get; }
        public string PostalCode { get; }
        public string Country { get; }
        public static Address None { get; } = new Address();
    }
}