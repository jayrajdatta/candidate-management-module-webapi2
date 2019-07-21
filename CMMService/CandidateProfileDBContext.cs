using System.Data.Entity;

namespace CMMService
{
    public class CandidateProfileDBContext : DbContext
    {
        public CandidateProfileDBContext() : base("CandidateDB")
        {
            //Database.SetInitializer<CandidateProfileDBContext>(new DropCreateDatabaseAlways<CandidateProfileDBContext>());
            Database.SetInitializer<CandidateProfileDBContext>(new DropCreateDatabaseIfModelChanges<CandidateProfileDBContext>());
        }

        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<ProfileDocument> ProfileDocuments { get; set; }

    }
}