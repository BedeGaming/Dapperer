using System.Collections.Generic;
using Dapperer.TestApiApp.DatabaseAccess;
using Dapperer.TestApiApp.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Dapperer.TestApiApp.Controllers
{
    [Route("contacts")]
    public class ContactsController : Controller
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
            var contact = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contact == null)
                return NotFound();

            _dbContext.ContactRepo.PopulateAddresses(contact);

            return Ok(contact);
        }

        [HttpPost("")]
        public IActionResult CreateContact(Contact contact)
        {
            var createdContact = _dbContext.ContactRepo.Create(contact);

            return CreatedAtRoute("Contact", new { contactId = createdContact.Id }, createdContact);
        }

        [HttpPut("{contactId:int}")]
        public IActionResult Update(int contactId, Contact contact)
        {
            var contactFromDb = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contactFromDb == null)
                return NotFound();

            contactFromDb.Name = contact.Name;
            _dbContext.ContactRepo.Update(contactFromDb);

            return Ok();
        }

        [HttpGet("{contactId:int}/addresses")]
        public IActionResult GetAllAddresses()
        {
            return Ok(_dbContext.AddressRepo.GetAll());
        }

        [HttpGet("{contactId:int}/addresses/{addressId:int}", Name = "Address")]
        public IActionResult GetAddressById(int contactId, int addressId)
        {
            var address = _dbContext.AddressRepo.GetSingleOrDefault(addressId);

            if (address == null)
                return NotFound();

            if (address.ContactId != contactId)
                return Forbid();

            _dbContext.AddressRepo.PopulateContact(address);

            return Ok(address);
        }

        [HttpPost("{contactId:int}/addresses")]
        public IActionResult CreateAddress(int contactId, Address address)
        {
            address.ContactId = contactId;
            var createdAddress = _dbContext.AddressRepo.Create(address);

            return CreatedAtRoute("Address", new { contactId = address.ContactId, addressId = createdAddress.Id }, createdAddress);
        }
    }
}