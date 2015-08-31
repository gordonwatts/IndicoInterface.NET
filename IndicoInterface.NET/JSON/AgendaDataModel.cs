using System;
using System.Collections.Generic;

namespace IndicoInterface.NET.JSON
{
    public class AdditionalInfo
    {
    }

    public class JDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class Person
    {
        public string _type { get; set; }
        public string emailHash { get; set; }
        public string affiliation { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public string id { get; set; }
    }

    public class ModificationDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class Note
    {
    }

    public class Attachment
    {
        public string _type { get; set; }
        public DateTime modified_dt { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string download_url { get; set; }
        public string filename { get; set; }
        public string content_type { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public int size { get; set; }
    }

    public class Folder
    {
        public string _type { get; set; }
        public IList<Attachment> attachments { get; set; }
        public string title { get; set; }
        public bool default_folder { get; set; }
        public int id { get; set; }
        public string description { get; set; }
    }

    public class Resource
    {
        public string _type { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public string _fossil { get; set; }
        public string id { get; set; }
        public bool _deprecated { get; set; }
    }

    public class Material
    {
        public string _type { get; set; }
        public string title { get; set; }
        public string _fossil { get; set; }
        public string id { get; set; }
        public IList<Resource> resources { get; set; }
        public bool _deprecated { get; set; }
    }

    public class SubContribution
    {
        public IList<Folder> folders { get; set; }
        public IList<Person> speakers { get; set; }
        public string title { get; set; }
        public IList<object> material { get; set; }
        public string _type { get; set; }
        public Note note { get; set; }
        public string _fossil { get; set; }
        public object allowed { get; set; }
        public int duration { get; set; }
        public string id { get; set; }
    }

    public class Contribution
    {
        public JDate startDate { get; set; }
        public JDate endDate { get; set; }
        public string session { get; set; }
        public IList<object> keywords { get; set; }
        public string id { get; set; }
        public IList<Person> speakers { get; set; }
        public IList<Person> primaryauthors { get; set; }
        public string title { get; set; }
        public Note note { get; set; }
        public object location { get; set; }
        public string _fossil { get; set; }
        public object type { get; set; }
        public IList<Folder> folders { get; set; }
        public string _type { get; set; }
        public string description { get; set; }
        public object track { get; set; }
        public IList<Material> material { get; set; }
        public IList<object> coauthors { get; set; }
        public IList<SubContribution> subContributions { get; set; }
        public object room { get; set; }
        public string url { get; set; }
        public string roomFullname { get; set; }
    }

    public class Visibility
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class CreationDate
    {
        public string date { get; set; }
        public string tz { get; set; }
        public string time { get; set; }
    }

    public class BookedRooms
    {

    }

    public class SupportInfo
    {
        public string _fossil { get; set; }
        public string caption { get; set; }
        public string _type { get; set; }
        public string email { get; set; }
        public string telephone { get; set; }
    }
    

    public class Convener
    {
        public string fax { get; set; }
        public string name { get; set; }
        public string firstName { get; set; }
        public string title { get; set; }
        public string affiliation { get; set; }
        public string familyName { get; set; }
        public string _type { get; set; }
        public int id { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string _fossil { get; set; }
        public string fullName { get; set; }
        public string email { get; set; }
    }
    
    public class Conference
    {
        public JDate startDate { get; set; }
        public string _type { get; set; }
        public JDate endDate { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string id { get; set; }
        public BookedRooms bookedRooms { get; set; }
        public string location { get; set; }
        public string address { get; set; }
        public string _fossil { get; set; }
        public string timezone { get; set; }
        public JDate adjustedEndDate { get; set; }
        public JDate adjustedStartDate { get; set; }
        public string type { get; set; }
        public SupportInfo supportInfo { get; set; }
        public object room { get; set; }
    }

    public class Session
    {
        public Conference conference { get; set; }
        public JDate startDate { get; set; }
        public string _type { get; set; }
        public JDate endDate { get; set; }
        public string description { get; set; }
        public Session session { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public IList<Convener> conveners { get; set; }
        public string slotTitle { get; set; }
        public Note note { get; set; }
        public object roomFullname { get; set; }
        public string location { get; set; }
        public IList<Folder> folders { get; set; }
        public bool inheritLoc { get; set; }
        public string _fossil { get; set; }
        public bool inheritRoom { get; set; }
        public IList<Contribution> contributions { get; set; }
        public string id { get; set; }
        public object address { get; set; }
        public string room { get; set; }
    }

    public class Result
    {
        public JDate startDate { get; set; }
        public JDate endDate { get; set; }
        public Person creator { get; set; }
        public bool hasAnyProtection { get; set; }
        public string roomFullname { get; set; }
        public ModificationDate modificationDate { get; set; }
        public string timezone { get; set; }
        public IList<Contribution> contributions { get; set; }
        public string id { get; set; }
        public string category { get; set; }
        public string title { get; set; }
        public Note note { get; set; }
        public object location { get; set; }
        public string _fossil { get; set; }
        public string type { get; set; }
        public string categoryId { get; set; }
        public IList<Folder> folders { get; set; }
        public string _type { get; set; }
        public string description { get; set; }
        public string roomMapURL { get; set; }
        public IList<Session> sessions { get; set; }
        public IList<Material> material { get; set; }
        public Visibility visibility { get; set; }
        public object address { get; set; }
        public CreationDate creationDate { get; set; }
        public object room { get; set; }
        public IList<Person> chairs { get; set; }
        public string url { get; set; }
    }

    public class IndicoGetMeetingInfoReturn
    {
        public int count { get; set; }
        public AdditionalInfo additionalInfo { get; set; }
        public string _type { get; set; }
        public string url { get; set; }
        public IList<Result> results { get; set; }
        public int ts { get; set; }
    }
}
