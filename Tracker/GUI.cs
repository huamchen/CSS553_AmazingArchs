using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace Tracker
{
    public partial class GUI : Form
    {
        Moniter mMoniter = new Moniter();
        DataSet ds=new DataSet();
        public GUI()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new connectionInfo(mMoniter).Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mMoniter.deleteConnection();
            MessageBox.Show("Unregiste Success!");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ds = mMoniter.getAllLog();
            MessageBox.Show("Get Log Success!");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            mMoniter.outputLog(ds);
            MessageBox.Show("Output Log Success!");
        }
    }

    public class Moniter
    {
        public List<SqlConnection> connections;
        public Register mRegister;
        public Receiver mReceiver;
        public Formatter mFormatter;
        public Moniter()
        {
            connections = new List<SqlConnection>();
            mRegister = new Register();
            mReceiver = new Receiver();
            mFormatter = new Formatter();
        }
        public Boolean createConnection(String dbUrl, String dbName, String dbUsername, String dbPassword, String keyword)
        {
            SqlConnection sqlConn = mRegister.createEvent(dbUrl,dbName, dbUsername, dbPassword, keyword);
            if (sqlConn == null)
            {
                return false;
            }
            connections.Add(sqlConn);
            return true;
        }
        public void deleteConnection()
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if(!mRegister.dropEvent(connections[i]))
                {
                    MessageBox.Show(connections[i].DataSource+" Unregiste Fail!");
                }
            }
            connections.Clear();
        }
        public DataSet getAllLog()
        {
            DataSet ds = new DataSet();
            for (int i = 0; i < connections.Count; i++)
            {
                ds.Merge(mReceiver.getLog(connections[i]));
            }
            return ds;
        }
        public void outputLog(DataSet ds)
        {
            mFormatter.outputLog(ds);
        }

    }
    public class Register
    {
        public SqlConnection createEvent(String dbUrl, String dbName, String dbUsername, String dbPassword, String keyword)
        {
            SqlConnection SqlConn = new SqlConnection("Data Source = "+dbUrl+ " ; Initial Catalog = " + dbName + "; User ID = " + dbUsername+" ; Password = "+dbPassword+" ");
            string str = "IF EXISTS (SELECT * from sys.database_event_sessions WHERE name = 'eventsession_gm_azuresqldb51') BEGIN DROP EVENT SESSION eventsession_gm_azuresqldb51 ON DataBase  END;";
            str+= "CREATE EVENT SESSION eventsession_gm_azuresqldb51  ON Database ADD EVENT sqlserver.sql_statement_completed ( ACTION(sqlserver.sql_text, sqlserver.tsql_stack, sqlserver.client_app_name, sqlserver.client_hostname, sqlserver.username) WHERE(statement LIKE '%"+keyword+"%')) ADD TARGET package0.ring_buffer (SET  max_memory = 500 );";
            str += "ALTER EVENT SESSION eventsession_gm_azuresqldb51 ON DATABASE STATE = START;";
            SqlCommand SqlCmd = new SqlCommand(str, SqlConn);
            try {
                SqlConn.Open();
                SqlCmd.ExecuteNonQuery();
                SqlConn.Close();
            }
            catch (Exception e)  
            {
                MessageBox.Show(e.Message);
                return null;
            }
            return SqlConn;
        }
        public Boolean dropEvent(SqlConnection SqlConn)
        {
            string str = "IF EXISTS (SELECT * from sys.database_event_sessions WHERE name = 'eventsession_gm_azuresqldb51') BEGIN ALTER EVENT SESSION eventsession_gm_azuresqldb51  ON DATABASE  STATE = STOP; ";
            str += " ALTER EVENT SESSION eventsession_gm_azuresqldb51   ON DATABASE     DROP TARGET package0.ring_buffer; ";
            str+=" DROP EVENT SESSION eventsession_gm_azuresqldb51  ON DATABASE; END;";
            SqlCommand SqlCmd = new SqlCommand(str, SqlConn);
            try
            {
                SqlConn.Open();
                SqlCmd.ExecuteNonQuery();
                SqlConn.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            return true;
        }
    }
    public class Receiver
    {
        public DataSet getLog(SqlConnection SqlConn)
        {
            string str = "SELECT timestamp, client_app_name, client_hostname,  username, duration,  sql_text FROM( SELECT DATEADD(hh,DATEDIFF(hh, GETUTCDATE(), CURRENT_TIMESTAMP),";
            str+="event_data.value('(event/@timestamp)[1]', 'datetime2')) AS [timestamp],  event_data.value('(event/action[@name="+"client_app_name"+"]/value)[1]', 'nvarchar(4000)') AS[client_app_name],";
            str += "event_data.value('(event/action[@name=\"client_hostname\"]/value)[1]', 'nvarchar(4000)') AS[client_hostname],event_data.value('(event/action[@name=\"username\"]/value)[1]', 'nvarchar(4000)') AS[username],";
            str += " event_data.value('(event/data[@name= \"duration\"]/value)[1]', 'bigint') AS[duration],REPLACE(event_data.value('(event/action[@name=\"sql_text\"]/value)[1]', 'nvarchar(max)'), CHAR(10), CHAR(13) + CHAR(10)) AS[sql_text]  ";
            str += "FROM (SELECT XEvent.query('.') AS event_data   FROM  (  SELECT CAST(target_data AS XML) AS TargetData   FROM sys.dm_xe_database_session_targets st JOIN sys.dm_xe_database_sessions s  ON s.address = st.event_session_address  WHERE name = 'eventsession_gm_azuresqldb51'  AND target_name = 'ring_buffer' ) AS Data    CROSS APPLY TargetData.nodes('RingBufferTarget/event') AS XEventData(XEvent)  ) AS tab (event_data)) AS results;";
            SqlCommand cmd = new SqlCommand(str, SqlConn);
            SqlDataAdapter sda = new SqlDataAdapter();
            sda.SelectCommand = cmd;

            DataSet ds = new DataSet();
            sda.Fill(ds);
            ds.Tables[0].Columns.Add("DatabaseURL", typeof(string));        
            for(int i=0;i< ds.Tables[0].DefaultView.Count;i++)
                ds.Tables[0].Rows[i]["DatabaseURL"] = SqlConn.DataSource;
            return ds;

        }
    }
    public class Formatter
    {
        public void outputLog(DataSet ds)
        {
            ds.WriteXml("E://a.xml");
        }
    }

}
