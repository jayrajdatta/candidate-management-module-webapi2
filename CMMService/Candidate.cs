using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMMService
{
    public class Candidate
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CandidateId { get; set; }

        [Required]
        [StringLength(250)]
        public string FullName { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(3000)]
        public string Domain { get; set; }

        public virtual ProfileDocument ProfileDocument { get; set; }
    }
}