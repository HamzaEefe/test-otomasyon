using Dapper;

namespace TestOtomasyon.Helpers
{
    public static class DapperConfig
    {
        public static void Configure()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = false;

            SqlMapper.SetTypeMap(typeof(Entities.Organization), new CustomPropertyTypeMap(typeof(Entities.Organization), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.Department), new CustomPropertyTypeMap(typeof(Entities.Department), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.User), new CustomPropertyTypeMap(typeof(Entities.User), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.Role), new CustomPropertyTypeMap(typeof(Entities.Role), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.UserRole), new CustomPropertyTypeMap(typeof(Entities.UserRole), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.Authority), new CustomPropertyTypeMap(typeof(Entities.Authority), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.RoleAuthority), new CustomPropertyTypeMap(typeof(Entities.RoleAuthority), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.WorkTask), new CustomPropertyTypeMap(typeof(Entities.WorkTask), MatchProperty));
            SqlMapper.SetTypeMap(typeof(Entities.Message), new CustomPropertyTypeMap(typeof(Entities.Message), MatchProperty));
        }

        private static System.Reflection.PropertyInfo? MatchProperty(Type type, string columnName)
        {
            return type.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));
        }
    }
}