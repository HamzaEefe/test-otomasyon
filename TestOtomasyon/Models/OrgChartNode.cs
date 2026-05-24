using TestOtomasyon.Entities;

namespace TestOtomasyon.Models
{
    public class OrgChartNode
    {
        public User User { get; set; } = null!;
        public int Level { get; set; }
        public List<OrgChartNode> Children { get; set; } = new();
    }
}