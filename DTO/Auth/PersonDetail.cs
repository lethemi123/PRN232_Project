namespace ProjectPRN232.DTO.Auth
{
    public class PersonDetail
    {
        public string UserName {get ; set; }
        public string FullName {get ; set; }
        public string email {get ; set; }
        public string phone {get ; set; }
        public string Gender {get ; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
}
