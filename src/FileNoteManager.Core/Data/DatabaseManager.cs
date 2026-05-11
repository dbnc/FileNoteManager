using Microsoft.Data.Sqlite;

namespace FileNoteManager.Core.Data;

/// <summary>
/// Manages database connections and provides connection pooling
/// </summary>
public class DatabaseManager : IDisposable
{
    private readonly string _connectionString;
    private readonly object _lock = new();
    private bool _disposed;
    
    public DatabaseManager(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        }.ToString();
        
        // Initialize database schema
        var initializer = new DatabaseInitializer(databasePath);
        initializer.Initialize();
    }
    
    /// <summary>
    /// Create a new database connection
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
    
    /// <summary>
    /// Execute a command and return the result
    /// </summary>
    public T ExecuteScalar<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(sql, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        var result = command.ExecuteScalar();
        return result == null || result == DBNull.Value 
            ? default! 
            : (T)result;
    }
    
    /// <summary>
    /// Execute a non-query command
    /// </summary>
    public int ExecuteNonQuery(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(sql, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        return command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Execute a query and return all results
    /// </summary>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(sql, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        var results = new List<T>();
        using var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            results.Add(MapReader<T>(reader));
        }
        
        return results;
    }
    
    /// <summary>
    /// Execute a query and return the first result
    /// </summary>
    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(sql, connection);
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        using var reader = command.ExecuteReader();
        
        if (reader.Read())
        {
            return MapReader<T>(reader);
        }
        
        return default;
    }
    
    private void AddParameters(SqliteCommand command, object parameters)
    {
        var properties = parameters.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(parameters);
            command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
        }
    }
    
    private T MapReader<T>(SqliteDataReader reader)
    {
        var type = typeof(T);
        var instance = Activator.CreateInstance<T>();
        
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            var property = type.GetProperty(name);
            
            if (property != null && !reader.IsDBNull(i))
            {
                var value = reader.GetValue(i);
                
                // Handle type conversions
                if (property.PropertyType == typeof(bool) && value is long l)
                {
                    property.SetValue(instance, l != 0);
                }
                else if (property.PropertyType == typeof(DateTime) && value is string s)
                {
                    property.SetValue(instance, DateTime.Parse(s));
                }
                else if (property.PropertyType == typeof(int) && value is long ln)
                {
                    property.SetValue(instance, (int)ln);
                }
                else
                {
                    property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
                }
            }
        }
        
        return instance;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // SQLite connections are managed by connection pooling
            // No explicit cleanup needed
            _disposed = true;
        }
    }
}
