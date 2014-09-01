using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using Dapperer.TestApiApp.DatabaseAccess;
using Dapperer.TestApiApp.Entities;

namespace Dapperer.TestApiApp.Controllers
{
    [RoutePrefix("contacts")]
    public class ContactsController : ApiController
    {
        private readonly IDbContext _dbContext;

        public ContactsController(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Route("")]
        public IEnumerable<Contact> GetAllContacts()
        {
            return _dbContext.ContactRepo.GetAll();
        }

        [Route("{contactId:int}", Name = "Contact")]
        public IHttpActionResult GetContactById(int contactId)
        {
            Contact contact = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contact == null)
                return NotFound();

            _dbContext.ContactRepo.PopulateAddresses(contact);

            return Ok(contact);
        }

        [Route("")]
        public IHttpActionResult CreateContact(Contact contact)
        {
            Contact createdContact = _dbContext.ContactRepo.Create(contact);

            return CreatedAtRoute("Contact", new { contactId = createdContact.Id }, createdContact);
        }

        [Route("{contactId:int}")]
        public IHttpActionResult Update(int contactId, Contact contact)
        {
            Contact contactFromDb = _dbContext.ContactRepo.GetSingleOrDefault(contactId);

            if (contactFromDb == null)
                return NotFound();

            contactFromDb.Name = contact.Name;
            _dbContext.ContactRepo.Update(contactFromDb);

            return Ok();
        }

        [Route("{contactId:int}/addresses")]
        public IEnumerable<Address> GetAllAddresses()
        {
            return _dbContext.AddressRepo.GetAll();
        }

        [Route("{contactId:int}/addresses/{addressId:int}", Name = "Address")]
        public IHttpActionResult GetAddressById(int contactId, int addressId)
        {
            Address address = _dbContext.AddressRepo.GetSingleOrDefault(addressId);

            if (address == null)
                return NotFound();

            if(address.ContactId != contactId)
                throw new HttpResponseException(HttpStatusCode.Forbidden);

            _dbContext.AddressRepo.PopulateContact(address);

            return Ok(address);
        }

        [Route("{contactId:int}/addresses")]
        public IHttpActionResult CreateAddress(int contactId, Address address)
        {
            address.ContactId = contactId;
            Address createdAddress = _dbContext.AddressRepo.Create(address);

            return CreatedAtRoute("Address", new { contactId = address.ContactId, addressId = createdAddress.Id }, createdAddress);
        }
    }
}