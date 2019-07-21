using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CMMService
{
    public class ProfileDocument
    {
        [Key, ForeignKey("Candidate")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CandidateId { get; set; }

        public string DocumentName { get; set; }

        public string DocumentType { get; set; }
        public byte[] DocumentData { get; set; }

        public virtual Candidate Candidate { get; set; }
    }
}