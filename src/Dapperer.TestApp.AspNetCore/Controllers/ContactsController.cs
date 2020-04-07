using System.Collections.Generic;
using System.Net;
using Dapperer.TestApp.AspNetCore.DatabaseAccess;
using Dapperer.TestApp.AspNetCore.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Dapperer.TestApp.AspNetCore.Controllers
{
    [Route("api/contracts")]
    public class ContactsController : ControllerBase
    {
        private readonly IDbContext _dbContext;

        public ContactsController(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("")]
        public IEnumerable<Contact> GetAllContacts()
        {
            return _dbContext.ContactRepo.GetAll();
        }

        [HttpGet("{contactId:int}", Name = "Contact")]
        public IActionResult GetContactById(int contactId)
        {
            Contact contact = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contact == null)
                return NotFound();

            _dbContext.ContactRepo.PopulateAddresses(contact);

            return Ok(contact);
        }

        [HttpPost("")]
        public IActionResult CreateContact(Contact contact)
        {
            Contact createdContact = _dbContext.ContactRepo.Create(contact);

            return CreatedAtRoute("Contact", new { contactId = createdContact.Id }, createdContact);
        }

        [HttpPut("{contactId:int}")]
        public IActionResult Update(int contactId, Contact contact)
        {
            Contact contactFromDb = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contactFromDb == null)
                return NotFound();

            contactFromDb.Name = contact.Name;
            _dbContext.ContactRepo.Update(contactFromDb);

            return Ok();
        }

        [HttpGet("{contactId:int}/addresses")]
        public IEnumerable<Address> GetAllAddresses()
        {
            return _dbContext.AddressRepo.GetAll();
        }

        [HttpGet("{contactId:int}/addresses/{addressId:int}", Name = "Address")]
        public IActionResult GetAddressById(int contactId, int addressId)
        {
            Address address = _dbContext.AddressRepo.GetSingleOrDefault(addressId);

            if (address == null)
                return NotFound();

            if (address.ContactId != contactId)
                return Forbid();

            _dbContext.AddressRepo.PopulateContact(address);

            return Ok(address);
        }

        [HttpGet("{contactId:int}/addresses")]
        public IActionResult CreateAddress(int contactId, Address address)
        {
            address.ContactId = contactId;
            Address createdAddress = _dbContext.AddressRepo.Create(address);

            return CreatedAtRoute("Address", new { contactId = address.ContactId, addressId = createdAddress.Id }, createdAddress);
        }
    }
}