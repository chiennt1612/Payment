using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Dtos
{
    public class ObjectsDTO
    {
        public ObjectsDTO()
        {
            Obj = new List<ObjectDTO>();
        }

        public List<ObjectDTO> Obj { get; set; }

        public int TotalCount { get; set; }

        public int PageSize { get; set; }
    }


    public class ClientPropertyApiDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
    public class ObjectDTO
    {
        public ObjectDTO()
        {
            //AllowedScopes = new List<string>();
            //PostLogoutRedirectUris = new List<string>();
            Properties = new List<ClientPropertyApiDto>();
        }

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientName { get; set; }

        public string ClientUri { get; set; }

        public string Description { get; set; }

        public bool Enabled { get; set; } = true;

        public List<ClientPropertyApiDto> Properties { get; set; }
    }
}
