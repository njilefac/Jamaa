using System;
using System.Collections.Generic;
using Domain.Entities.Finances;
using Domain.Entities.Members;
using Domain.Values;

namespace Domain.Entities.Shared
{
    public class Organization
    {
        /// <summary>
        /// A uid that uniquely identifies the organization
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// the name of the organization
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// a short description of the organization. 
        /// </summary>
        public string Description { get; }

        public ISet<Member> Members { get; }

        /// <summary>
        ///  the set of fees that are applicable for the organization.
        /// </summary>
        public ISet<IFee> Fees { get; }

        /// <summary>
        /// Creates a new Organization or Association.
        /// </summary>
        /// <param name="name">the name of the organization</param>
        /// <param name="description">a short description of the purpose of the organization</param>
        public Organization(string name, string description)
        {
            Name = name;
            Description = description;
            Members = new HashSet<Member>();
        }

        public Registration Register(Member member, MembershipType membershipType, DateTime registrationDate)
        {
            Members.Add(member);

            return new Registration(member, membershipType, registrationDate, null);
        }

        /// <summary>
        /// Adds a new <see cref="IFee"/> to the organization 
        /// </summary>
        /// <param name="fee">the new fee to be introduced</param>
        /// <returns>a reference to the new <see cref="IFee"/> that was added</returns>
        public IFee IntroduceFee(IFee fee)
        {
            throw new NotImplementedException();
        }
    }
}
