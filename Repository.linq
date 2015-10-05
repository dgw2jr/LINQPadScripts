<Query Kind="Program">
  <Output>DataGrids</Output>
  <GACReference>Microsoft.VisualStudio.QualityTools.UnitTestFramework, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</GACReference>
  <NuGetReference>Autofac</NuGetReference>
  <Namespace>Autofac</Namespace>
  <Namespace>Autofac.Builder</Namespace>
  <Namespace>Microsoft.VisualStudio.TestTools.UnitTesting</Namespace>
</Query>

private const bool IsTest = false;

private static IContainer Container { get; set; }

void Main()
{
	RegisterComponents();

	DumpListOf<CodeSource>();
	DumpListOf<Edition>();
}

void RegisterComponents()
{
	var builder = new ContainerBuilder();
	builder.RegisterGeneric(typeof(LocalDbConnector<>)).AsImplementedInterfaces();
	builder.RegisterGeneric(typeof(Repository<>)).AsImplementedInterfaces();
	builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(t => t.Name.EndsWith("Factory")).AsImplementedInterfaces();
	
	//override codesource with new version
	builder.RegisterType<CodeSourceV1Factory>().As<IFactory<CodeSource>>();

	if (IsTest)
	{
		builder.RegisterGeneric(typeof(MockRepository<>)).AsImplementedInterfaces();
	}

	Container = builder.Build();
}

void DumpListOf<T>()
{
	var list = new List<T>();
	using (var lifetime = Container.BeginLifetimeScope())
	{
		using (var conn = lifetime.Resolve<IDbConnector<T>>().Connection)
		{
			var repo = lifetime.Resolve<IRepository<T>>();

			foreach (var i in Enumerable.Range(1, 20))
			{
				list.Add(repo.Get(i));
			}
		}
	}

	list.Dump();
}

#region Factories
public interface IFactory<T>
{
	string Catalog { get; }
	string TableName { get; }
	string KeyColumn { get; }
	T Create(IDataReader result);
}

public class CodeSourceV1Factory : CodeSourceFactory
{
	public override string Catalog
	{
		get { return "EncoderServices"; }
	}
}

public class CodeSourceV2Factory : CodeSourceFactory
{
	public override string Catalog { get { return "TestingGrounds"; } }
}

public abstract class CodeSourceFactory : IFactory<CodeSource>
{
	public virtual string Catalog { get { return "EncoderServices"; } }
	public virtual string TableName { get { return "CodeSources"; } }
	public virtual string KeyColumn { get { return "CodeSourceId"; } }

	public virtual CodeSource Create(IDataReader result)
	{
		var codeSource = new CodeSource();
		codeSource.CodeSourceName = result["CodeSourceName"].ToString();
		codeSource.Id = Convert.ToInt32(result["CodeSourceId"]);

		return codeSource;
	}
}

public class EditionV1Factory : EditionFactory { }

public abstract class EditionFactory : IFactory<Edition>
{
	public virtual string Catalog { get { return "EncoderServices"; } }
	public virtual string TableName { get { return "Editions"; } }
	public virtual string KeyColumn { get { return "EditionId"; } }

	public virtual Edition Create(IDataReader result)
	{
		var edition = new Edition();
		edition.EditionName = result["EditionName"].ToString();
		edition.Id = Convert.ToInt32(result["EditionId"]);

		return edition;
	}
}
#endregion

#region Repositories
public interface IRepository<T>
{
	T Get(int id);
}

public class Repository<T> : IRepository<T> where T : IEntity
{
	private IDbConnector<T> _connector;
	private IFactory<T> _entityFactory;
	public Repository(IDbConnector<T> connector, IFactory<T> entityFactory)
	{
		_connector = connector;
		_entityFactory = entityFactory;
	}

	public T Get(int id)
	{
		T entity = default(T);
		var cmd = _connector.Connection.CreateCommand();
		cmd.CommandText = string.Format("select * from {0} where {1} = @ID", _entityFactory.TableName, _entityFactory.KeyColumn);
		cmd.Parameters.Add(new SqlParameter("ID", id));

		var result = cmd.ExecuteReader();
		while (result.Read())
		{
			entity = _entityFactory.Create(result);
		}
		_connector.Connection.Close();

		return entity;
	}
}

public class MockRepository<T> : IRepository<T> where T : IEntity, new()
{
	public MockRepository()
	{
	}

	public T Get(int id)
	{
		return new T { Id = id };
	}
}
#endregion

#region Entities
public interface IEntity
{
	int Id { get; set; }
}

public class CodeSource : IEntity
{
	public int Id { get; set; }
	public string CodeSourceName { get; set; }
}

public class Edition : IEntity
{
	public int Id { get; set; }
	public string EditionName { get; set; }
}
#endregion

public interface IDbConnector<T>
{
	IDbConnection Connection { get; }
}

public class LocalDbConnector<T> : IDbConnector<T>
{
	private IFactory<T> _entityFactory;
	public LocalDbConnector(IFactory<T> entityFactory)
	{
		_entityFactory = entityFactory;
	}
	
	public IDbConnection Connection
	{ 
		get 
		{ 
			var conn = new SqlConnection(connectionString);
			conn.Open();
			return conn;
		} 
	}

	private string connectionString
	{
		get
		{
			return string.Format("Data Source=.\\;Initial Catalog={0};Integrated Security=true;", _entityFactory.Catalog);
		}
	}
}