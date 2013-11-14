/* ======================================================================
 * Copyright (C) 2004-2005 zhaixd@hotmail.com. All rights reserved.
 * FileName		 : IEntityContext.cs
 * Author		 : ��ѩ��
 * Date			 : 2004-12-24
 * Version		 : 1.0
 * 
 * This software is the confidential and proprietary information of 
 * zhaixd@hotmail.com ("Confidential Information").  
 * You shall not disclose such Confidential Information and shall
 * use it only in accordance with the terms of the license agreement 
 * you entered into with zhaixd@hotmail.com.
 * ======================================================================
 * History (��ʷ�޸ļ�¼)
 *	<author>	   <time>		<version>	<description>
 *	��ѩ��		2004-12-24		   1.0		build this moudle  
 */

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;

using XMS.Core.Entity;

namespace XMS.Core.Data
{
	/// <summary>
	/// <b>DataTableAdapter</b> �ṩ DataTable �����ݿ�֮���ͨ�š�
	/// </summary>
	/// <remarks>
	/// ֻ��ʹ�� <see cref="IEntityContext"/> �� <see cref="IDatabase.CreateDataTableAdapter()"/> �������� <see cref="DataTableAdapter"/> ��ʵ����
	/// <b>DataTableAdapter</b> ͨ�� <b>IEntityContext</b> �ṩ�����Ӽ����������ʼ������Ļ��������ݿ����ͨ�š�ִ�в�ѯ��洢���̣�
	/// �������÷����������������ݱ�����÷�������������� <b>DataTableAdapter</b>��
	/// <b>DataTableAdapter</b> �����ڽ��������ݴ�Ӧ�ó����ͻ����ݿ⡣
	/// </remarks>
	/// <example>
	/// ��ʼ����
	/// 	DataTableAdapter adapter = entityContext.CreateDataTableAdapter();
	/// ����:
	/// 	DataTabale table = new DataTable(entityContext.GetPartitionTableName("Order"));
	///   ����Ҫ����ʱ��
	/// 	adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>10", parameters).Fill(table);
	///   ������д��ʱ��
	///		Dictionary&lt;string, object&gt; parameters = new Dictionary&lt;string, object&gt;(1);
	///		parameters["OrderId"] = 10;  ���� parameters["@OrderId"] = 10;
	///		adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>@OrderId", parameters).Fill(table);
	///	  ��������д��ʱ��
	///		adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>@OrderId", 
	///			new System.Data.Common.DbParameter[]{
	///				adapter.CreateParameter("OrderId", DbType.Int32, 10)
	///			}).Fill(table);
	/// ʹ���Զ�������¶Ա�ĸ���:
	///		adapter.BuildCommands().Update(table);
	/// ʹ���Զ���������±�:
	///	  1.����Update����:
	///		������д����
	///		adapter.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			"Title", "CustomerName", "OrderId", "A");
	///		��������д����
	///		adapter.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			new System.Data.Common.DbParameter[]{
	///					adapter.CreateParameter("Title", DbType.String, 100),
	///					adapter.CreateParameter("@CustomerName", DbType.String, 200),
	///					adapter.CreateParameter("OrderId", DbType.Int32)
	///			});
	///	  2.����Delete��� �򻯺���������д���ֱ�ο�Update����
	///		adapter.SetDeleteCommand(...);
	///	  3.����Insert��� �򻯺���������д���ֱ�ο�Update����
	///		adapter.SetInsertCommand(...);
	///	  4.ִ�и���	
	///		this.dataTableAdapter.Update(table); ���� this.dataTableAdapter.Update(table.Rows); ���� this.dataTableAdapter.Update(dataset, tableName);
	///	�����Զ������ִ��
	///		using(DbCommand command = adapter.CreateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			new System.Data.Common.DbParameter[]{
	///					adapter.CreateParameter("Title", DbType.String, 100),
	///					adapter.CreateParameter("@CustomerName", DbType.String, 200),
	///					adapter.CreateParameter("OrderId", DbType.Int32)
	///			})
	///		{
	///			foreach(var item in list)
	///			{
	///				command.Parameters["@Title"] = item.Title;
	///				command.Parameters["@CustomerName"] = item.CustomerName;
	///				command.Parameters["@OrderId"] = item.OrderId;
	///				
	///				command.ExecuteNoneQuery();
	///			}
	///		}
	/// </example>
	public class DataTableAdapter : IDisposable
	{
		/// <summary>
		/// Specifies the action that command is supposed to perform, i.e. Select, Insert, Update, Delete.
		/// It is used in Execute methods of the <see cref="IEntityContext"/> class to identify command instance 
		/// to be used.
		/// </summary>
		private enum CommandAction
		{
		    Select,
		    Insert,
		    Update,
		    Delete,
			Other
		}

		private DbCommand _selectCommand = null;
		private DbCommand _insertCommand = null;
		private DbCommand _updateCommand = null;
		private DbCommand _deleteCommand = null;
		private DbParameter[] _selectCommandParameters;
		private DbParameter[] _insertCommandParameters;
		private DbParameter[] _updateCommandParameters;
		private DbParameter[] _deleteCommandParameters;

		#region Public Properties
		/// <summary>
		/// Gets the select <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>SelectCommand</b> can be used to access select command parameters.
		/// </remarks>
		protected DbCommand SelectCommand
		{
			get
			{
				return this._selectCommand;
			}
		}

		/// <summary>
		/// Gets the insert <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>InsertCommand</b> can be used to access insert command parameters.
		/// </remarks>
		protected DbCommand InsertCommand
		{
			get
			{
				return this._insertCommand;
			}
		}

		/// <summary>
		/// Gets the update <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>UpdateCommand</b> can be used to access update command parameters.
		/// </remarks>
		protected DbCommand UpdateCommand
		{
			get
			{
				return this._updateCommand;
			}
		}

		/// <summary>
		/// Gets the delete <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>DeleteCommand</b> can be used to access delete command parameters.
		/// </remarks>
		protected DbCommand DeleteCommand
		{
			get
			{
				return this._deleteCommand;
			}
		}
		#endregion

		private DbProviderFactory dbProviderFactory;
		private DbConnection connection;
		private DbTransaction transaction;

		internal DataTableAdapter(DbProviderFactory dbProviderFactory, DbConnection connection, DbTransaction transaction)
		{
			if (dbProviderFactory == null)
			{
				throw new ArgumentNullException("dbProviderFactory");
			}
			if (connection == null)
			{
				throw new ArgumentNullException("connection");
			}
			this.dbProviderFactory = dbProviderFactory;
			this.connection = connection;
			this.transaction = transaction;

			switch (this.connection.State)
			{
				case ConnectionState.Closed:
					this.connection.Open();
					break;
			}
		}

		#region IDisposable interface
		private bool disposed = false;
		// Implement Idisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		//�� Close ����ʵ�ֵĹ�����ͬ
		public void Dispose()
		{
			Dispose(true);
			// Take yourself off of the Finalization queue 
			// to prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="IEntityContext"/> and 
		/// optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing"><b>true</b> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this._selectCommand != null)
					{
						this._selectCommand.Dispose();
						this._selectCommand = null;
					}
					if (this._insertCommand != null)
					{
						this._insertCommand.Dispose();
						this._insertCommand = null;
					}
					if (this._updateCommand != null)
					{
						this._updateCommand.Dispose();
						this._updateCommand = null;
					}
					if (this._deleteCommand != null)
					{
						this._deleteCommand.Dispose();
						this._deleteCommand = null;
					}
					if (this._adapter != null)
					{
						this._adapter.Dispose();
						this._adapter = null;
					}
					if (this._builder != null)
					{
						this._builder.Dispose();
						this._builder = null;
					}
				}
			}
			disposed = true;
		}
		~DataTableAdapter()
		{
			Dispose(false);
		}
		#endregion

		#region ��������������ִ�����ݿ��ѯ��������ɾ�Ĳ�����
		private DbCommand CreateCommand()
		{
			DbCommand cmd = this.connection.CreateCommand();
			if (this.transaction != null)
			{
				cmd.Transaction = this.transaction;
			}
			return cmd;
		}
		private DbCommand GetCommand(CommandAction commandAction)
		{
			switch (commandAction)
			{
				case CommandAction.Insert:
					if (this._insertCommand == null)
					{
						this._insertCommand = this.CreateCommand();
					}
					return this._insertCommand;
				case CommandAction.Update:
					if (this._updateCommand == null)
					{
						this._updateCommand = this.CreateCommand();
					}
					return this._updateCommand;
				case CommandAction.Delete:
					if (this._deleteCommand == null)
					{
						this._deleteCommand = this.CreateCommand();
					}
					return this._deleteCommand;
				case CommandAction.Select:
					if (this._selectCommand == null)
					{
						this._selectCommand = this.CreateCommand();
					}
					return this._selectCommand;
				default:
					return this.CreateCommand();
			}
		}

		private DbCommand SetCommandInternal(CommandAction cmdAction, CommandType cmdType, string cmdText, DbParameter[] cmdParameters, bool autoPrepareCommand)
		{
			DbCommand command = this.GetCommand(cmdAction);

			command.Parameters.Clear();
			command.CommandType = cmdType;

			command.CommandText = this.PretreatmentCommandText(cmdText);

			switch (cmdAction)
			{
				case CommandAction.Select:
					_selectCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.Dispose();
						this._adapter = null;
					}
					if (this._builder != null)
					{
						this._builder.Dispose();
						this._builder = null;
					}
					break;
				case CommandAction.Insert:
					_insertCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.InsertCommand = command;
					}
					break;
				case CommandAction.Update:
					_updateCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.UpdateCommand = command;
					}
					break;
				case CommandAction.Delete:
					_deleteCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.DeleteCommand = command;
					}
					break;
				default:
					break;
			}
			if (cmdParameters != null)
			{
				for (int i = 0; i < cmdParameters.Length; i++)
				{
					command.Parameters.Add(cmdParameters[i]);
				}
			}
			if (autoPrepareCommand)
			{
				command.Prepare();
			}
			return command;
		}

		private DataTableAdapter SetCommand(CommandAction cmdAction, CommandType cmdType, string cmdText, DbParameter[] cmdParameters, bool autoPrepareCommand)
		{
			this.SetCommandInternal(cmdAction, cmdType, cmdText, cmdParameters, autoPrepareCommand);

			return this;
		}

		public DbCommand CreateCommand(string commandText)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, null), false);
		}

		public DbCommand CreateCommand(string commandText, Dictionary<string, object> parameters)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		public DbCommand CreateCommand(string commandText, DbParameter[] parameters)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}


		#region ���������д�����Զ��ƶϲ�������
		/// <summary>
		/// ���ò���Ҫ�κβ����ķǲ�������ѯ���
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetSelectCommand(string commandText)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, null), false);
		}

		/// <summary>
		/// ���ò�ѯ�����ѯ�������Ϊ������ʹ�õ���ÿ������ָ������ֵ��
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetSelectCommand(string commandText, Dictionary<string, object> parameters)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// ���ò���������������Ϊ��������Ҫ��ÿ������ָ������ֵ�������ڵ��� Update ����ʱ���� DataTable ���� DataRow �Զ��ƶϲ���ֵ��
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetInsertCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Insert, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// ���ø���������������Ϊ��������Ҫ��ÿ������ָ������ֵ�������ڵ��� Update ����ʱ���� DataTable ���� DataRow �Զ��ƶϲ���ֵ��
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetUpdateCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Update, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// ����ɾ�����ɾ�������Ϊ��������Ҫ��ÿ������ָ������ֵ�������ڵ��� Update ����ʱ���� DataTable ���� DataRow �Զ��ƶϲ���ֵ��
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetDeleteCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Delete, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}
		#endregion

		#region �����������д������ʾ���ò�ѯ��������Ҫ�Ĳ��������������͡����ȡ�ӳ���е���Ϣ���������ɺ��Զ�Ϊ����� Prepare ��������ߺ����ظ�����ʱ�����ܡ�
		/// <summary>
		/// ���ò�ѯ�����ʾ���ò�ѯ��������Ҫ�Ĳ��������������͡����ȡ�ӳ���е���Ϣ���������ɺ��Զ�Ϊ����� Prepare ��������ߺ����ظ�����ʱ�����ܡ�
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetSelectCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// ���ò��������ʾ���ò�ѯ��������Ҫ�Ĳ��������������͡����ȡ�ӳ���е���Ϣ���������ɺ��Զ�Ϊ����� Prepare ��������ߺ����ظ�����ʱ�����ܡ�
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetInsertCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Insert, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// ���ø��������ʾ���ò�ѯ��������Ҫ�Ĳ��������������͡����ȡ�ӳ���е���Ϣ���������ɺ��Զ�Ϊ����� Prepare ��������ߺ����ظ�����ʱ�����ܡ�
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetUpdateCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Update, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// ����ɾ�������ʾ���ò�ѯ��������Ҫ�Ĳ��������������͡����ȡ�ӳ���е���Ϣ���������ɺ��Զ�Ϊ����� Prepare ��������ߺ����ظ�����ʱ�����ܡ�
		/// </summary>
		/// <param name="commandText">Ҫִ�е������ı���</param>
		/// <param name="parameters">ִ����������Ҫ�Ĳ������顣</param>
		/// <returns>��ǰ DataTableAdapter ʵ����</returns>
		public DataTableAdapter SetDeleteCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Delete, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}
		#endregion

		#region �����������д��ʱ������������䴴���������
		/// <summary>
		/// ����ǿ���͵� <see cref="DbParameter"/> ʵ���� 
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="value">����ֵ</param>
		/// <returns><see cref="DbParameter"/> ����ǿ����ʵ����</returns>
		public DbParameter CreateParameter(string parameterName, object value =null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.Value = value == null ? DBNull.Value : value;

			return p;
		}

		/// <summary>
		/// ����ǿ���͵� <see cref="DbParameter"/> ʵ���� 
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="dbType">��������</param>
		/// <returns><see cref="DbParameter"/> ����ǿ����ʵ����</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}

		/// <summary>
		/// ����ʵ�� <see cref="DbParameter"/> ����ṩ��������һ����ʵ����
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="dbType">��������</param>
		/// <param name="isNullable">���������ܿ�ֵ����Ϊ <b>true</b>������Ϊ <b>false</b>��</param>
		/// <returns><see cref="DbParameter"/> ����ʵ����</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, bool isNullable, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.IsNullable = isNullable;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		/// <summary>
		/// ����ǿ���͵� <see cref="DbParameter"/> ʵ���� 
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="dbType">��������</param>
		/// <param name="size">������С</param>
		/// <returns><see cref="DbParameter"/> ����ǿ����ʵ����</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, int size, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.Size = size;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		/// <summary>
		/// ����ǿ���͵� <see cref="DbParameter"/> ʵ���� 
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="dbType">��������</param>
		/// <param name="size">������С</param>
		/// <param name="sourceColumn">Դ�е�����</param>
		/// <returns><see cref="DbParameter"/> ����ǿ����ʵ����</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, int size, string sourceColumn, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName.Substring(1) : sourceColumn;
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName : sourceColumn;
			}

			p.DbType = dbType;

			p.Size = size;

			return p;
		}
		/// <summary>
		/// ����ǿ���͵� <see cref="DbParameter"/> ʵ���� 
		/// </summary>
		/// <param name="parameterName">��������</param>
		/// <param name="dbType">��������</param>
		/// <param name="size">������С</param>
		/// <param name="parameterDirection">�����ķ���</param>
		/// <param name="isNullable">���Դ�п�Ϊ�գ���Ϊ <b>true</b>������Ϊ <b>false</b>��</param>
		/// <param name="precision">ֵ�ľ���</param>
		/// <param name="scale">ֵ��С��λ��</param>
		/// <param name="sourceColumn">Դ�е�����</param>
		/// <param name="sourceVersion">ԭ�еİ汾</param>
		/// <param name="value">������ֵ</param>
		/// <returns><see cref="DbParameter"/> ����ǿ����ʵ����</returns>
		public DbParameter CreateParameter(
			string parameterName,
			DbType dbType,
			int size,
			ParameterDirection parameterDirection,
			bool isNullable,
			byte precision,
			byte scale,
			string sourceColumn,
			DataRowVersion sourceVersion,
			object value =null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName.Substring(1) : sourceColumn;
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName : sourceColumn;
			}

			p.DbType = dbType;
			p.Size = size;
			p.Direction = parameterDirection;
			p.IsNullable = isNullable;
			((IDbDataParameter)p).Precision = precision;
			((IDbDataParameter)p).Scale = scale;
			p.SourceVersion = sourceVersion;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		#endregion
		#endregion

		private DbDataAdapter _adapter = null;
		private DbCommandBuilder _builder = null;

		/// <summary>
		/// ��ȡ�������ݿ���ʵ�������������
		/// </summary>
		protected virtual DbDataAdapter Adapter
		{
			get
			{
				if (this._adapter == null)
				{
					this._adapter = this.dbProviderFactory.CreateDataAdapter();
					this._adapter.SelectCommand = this.SelectCommand;
					this._adapter.InsertCommand = this.InsertCommand;
					this._adapter.DeleteCommand = this.DeleteCommand;
					this._adapter.UpdateCommand = this.UpdateCommand;
				}
				return this._adapter;
			}
		}

		/// <summary>
		/// ��ȡ���ڴ��� SQL ִ���������������
		/// </summary>
		protected virtual DbCommandBuilder Builder
		{
			get
			{
				return this._builder;
			}
		}

		/// <summary>
		/// Ϊ���������������Զ�����
		/// </summary>
		/// <remarks>
		/// �Զ���������������Զ���Ĳ��롢ɾ������������
		/// </remarks>
		public DataTableAdapter BuildCommands()
		{
			if (this._builder == null)
			{
				if (this.InsertCommand == null || this.UpdateCommand == null || this.DeleteCommand == null)
				{
					this._builder = this.dbProviderFactory.CreateCommandBuilder();
					this._builder.DataAdapter = this.Adapter;
				}
			}
			return this;
		}

		/// <summary>
		/// ִ���� <see cref="SelectCommand"/> ָ��������� <see cref="DataTable"/> ����ӻ�ˢ������ƥ��ʹ�� <b>DataTable</b> ���Ƶ�����Դ�е��С�
		/// </summary>
		/// <param name="dataTable">Ҫ�ü�¼�ͼܹ��������Ҫ������ <see cref="DataTable"/>��</param>
		/// <returns>������ <see cref="DataTable"/>��</returns>
		public int Fill(DataTable dataTable)
		{
			return this.Adapter.Fill(dataTable);
		}

		#region Update
		/// <summary>
		/// Ϊָ�� <see cref="DataSet"/> ��ָ�����Ƶı��ÿ���Ѳ��롢�Ѹ��»���ɾ�����е�����Ӧ�� <b>INSERT</b>��<b>UPDATE</b> �� <b>DELETE</b> ��䡣
		/// </summary>
		/// <param name="dataSet">���ڸ�������Դ�� <see cref="DataSet"/>�� </param>
		/// <param name="tableName">���ڱ�ӳ���Դ������ơ�</param>
		/// <returns><see cref="DataSet"/> �гɹ����µ�������</returns>
		/// <remarks>
		/// <b>Update(DataSet,string,bool)</b>�������� <c>DbDataAdapter.Update(DataSet)</c> �������и��¡�
		/// <para>�����Ҫ�ֶ����������δָ�� INSERT��UPDATE �� DELETE ��䣬<b>Update</b> �����������쳣��
		/// ������������� <see cref="SelectCommand"/> ���ԣ�����Դ��� <b>CommandBuilder</b>Ϊ����������Զ����� SQL ��䡣
		/// Ȼ��CommandBuilder �����������κ�δ���õ� SQL ��䡣�������߼�Ҫ�� <b>DataSet</b> �д��ڼ�����Ϣ��</para>
		/// <para>����ʹ�� <see cref="SetInsertCommand"/>��<see cref="SetUpdateCommand"/>��<see cref="SetDeleteCommand"/> ������ʾָ�� INSERT��UPDATE �� DELETE ��䡣
		/// ʹ�÷���ʾ����<br/><c>this.SetInsertCommand(string,DbParameter[])</c> ��</para>
		/// </remarks>
		public int Update(DataSet dataSet, string tableName)
		{
			if (dataSet == null)
			{
				throw new ArgumentNullException("dataSet");
			}

			if (tableName == null)
			{
				return this.Adapter.Update(dataSet);
			}
			else
			{
				return this.Adapter.Update(dataSet, tableName);
			}
		}
		/// <summary>
		/// Ϊָ�� <see cref="DataTable"/> ��ÿ���Ѳ��롢�Ѹ��»���ɾ�����е�����Ӧ�� <b>INSERT</b>��<b>UPDATE</b> �� <b>DELETE</b> ��䡣
		/// </summary>
		/// <param name="dataTable">���ڸ�������Դ�� <see cref="DataTable"/>��</param>
		/// <returns><see cref="DataTable"/> �гɹ����µ�������</returns>
		/// <remarks>
		/// <b>Update(DataTable,bool)</b>�������� <c>DbDataAdapter.Update(DataTable)</c> �������и��¡�
		/// <para>�����Ҫ��ʾ���������δָ�� INSERT��UPDATE �� DELETE ��䣬<b>Update</b> �����������쳣��
		/// ������������� <see cref="SelectCommand"/> ���ԣ�����Դ��� <b>CommandBuilder</b>Ϊ����������Զ����� SQL ��䡣
		/// Ȼ��CommandBuilder �����������κ�δ���õ� SQL ��䡣�������߼�Ҫ�� <b>DataSet</b> �д��ڼ�����Ϣ��</para>
		/// <para>����ʹ�� <see cref="SetInsertCommand"/>��<see cref="SetUpdateCommand"/>��<see cref="SetDeleteCommand"/> ������ʾָ�� INSERT��UPDATE �� DELETE ��䡣
		/// ʹ�÷���ʾ����<br/><c>this.SetInsertCommand(string,DbParameter[])</c> ��</para>
		/// </remarks>
		public int Update(DataTable dataTable)
		{
			if (dataTable == null)
			{
				throw new ArgumentNullException("dataTable");
			}

			return this.Adapter.Update(dataTable);
		}

		/// <summary>
		/// Ϊָ�� <see cref="DataRow"/> ������ÿ���Ѳ��롢�Ѹ��»���ɾ�����е�����Ӧ�� <b>INSERT</b>��<b>UPDATE</b> �� <b>DELETE</b> ��䡣
		/// </summary>
		/// <param name="rows">���ڸ�������Դ��<see cref="DataRow"/> ���顣</param>
		/// <returns><see cref="DataRow"/> �����гɹ����µ�������</returns>
		public int Update(DataRow[] rows)
		{
			if (rows == null)
			{
				throw new ArgumentNullException("rows");
			}

			return this.Adapter.Update(rows);
		}
		#endregion

		#region ���·����� DataManager ���������Ҫ��������Ϊ Ado.Net ���ܵľۺ���
		private static System.Text.RegularExpressions.Regex regexParameter = new System.Text.RegularExpressions.Regex(@"@\w+");
		/// <summary>
		/// �Խ�Ҫִ�е��������Ԥ������ȷ����������ڲ�ͬ�����ݿ���ִ�У���ͨ������ֱ�Ӷ� DbCommand.CommandText ���и�ֵ�ĳ��ϡ�
		/// </summary>
		/// <param name="commandText"></param>
		/// <returns></returns>
		/// <example>
		/// �����������£���Ҫʹ�ô˷������������Ԥ����
		/// this._insertCommand = this.dataManager.Connection.CreateCommand();
		/// this._insertCommand.Connection = this.dataManager.Connection;
		/// this._insertCommand.Transaction = this.dataManager.Transaction;
		/// this._insertCommand.CommandType = System.Data.CommandType.Text;
		/// this._insertCommand.CommandText = this.dataManager.PretreatmentCommandText("INSERT INTO S_MESSAGES(TO_NUMBER,MESSAGE,SEND_TIME,ORG_ID,FLAG) VALUES (@TO_NUMBER,@MESSAGE,@SEND_TIME,@ORG_ID,@FLAG)");
		/// </example>
		internal string PretreatmentCommandText(string commandText)
		{
			if (this.dbProviderFactory.GetType() == typeof(System.Data.SqlClient.SqlClientFactory))
			{
				return commandText;
			}
			return regexParameter.Replace(commandText, "?");
		}

		/// <summary>
		/// �ж�ָ���������Ƿ�洢����
		/// </summary>
		/// <param name="commandText"></param>
		/// <returns></returns>
		private static bool IsStoreProcedure(string commandText)
		{
			return commandText.IndexOf(" ") == -1;
		}
		#endregion
	}
}