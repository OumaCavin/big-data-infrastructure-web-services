# Hadoop Installation Guide

**Author**: Cavin Otieno  
**Course**: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing  
**Date**: 2025-11-27  

---

## Overview

This guide provides step-by-step instructions for installing and configuring Apache Hadoop 3.3.6 in pseudo-distributed mode for educational purposes. The installation is designed to work on Linux, macOS, and Windows (with WSL).

## Prerequisites

### System Requirements

- **Operating System**: Linux (Ubuntu 18.04+), macOS (10.14+), or Windows 10+ with WSL2
- **Memory**: Minimum 4GB RAM (8GB recommended)
- **Storage**: Minimum 10GB free disk space
- **Network**: Internet connection for downloading Hadoop

### Software Dependencies

1. **Python 3.7+** (required for scripts)
2. **Java Development Kit (JDK) 8** (OpenJDK recommended)
3. **SSH** (for remote access and password-less login)

### Quick Prerequisites Check

```bash
# Check Python version
python3 --version

# Check Java installation
java -version

# Check available memory
free -h  # Linux
vm_stat  # macOS

# Check disk space
df -h
```

## Installation Methods

### Method 1: Automated Installation (Recommended)

The easiest way to install Hadoop is using the provided Python script:

```bash
# Make the script executable
chmod +x hadoop_setup.py

# Run the installation script
python3 hadoop_setup.py
```

The script will:
1. ✅ Check system prerequisites
2. ☕ Install Java if needed
3. 📦 Download Hadoop 3.3.6
4. 📂 Extract and configure Hadoop
5. 🔧 Setup environment variables
6. 🧪 Test the installation
7. 📝 Create convenient startup scripts

### Method 2: Manual Installation

Follow these steps for manual installation:

#### Step 1: Install Java

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install openjdk-8-jdk
```

**CentOS/RHEL:**
```bash
sudo yum install java-1.8.0-openjdk-devel
```

**macOS:**
```bash
brew install openjdk@8
```

**Verify Java Installation:**
```bash
java -version
javac -version
```

#### Step 2: Set JAVA_HOME

Add to your `~/.bashrc` or `~/.zshrc`:

```bash
# For OpenJDK 8 on Ubuntu/Debian
export JAVA_HOME=/usr/lib/jvm/java-8-openjdk-amd64

# For macOS with Homebrew
export JAVA_HOME=/usr/local/opt/openjdk@8

# Add Java to PATH
export PATH=$JAVA_HOME/bin:$PATH
```

#### Step 3: Download and Extract Hadoop

```bash
# Download Hadoop
wget https://archive.apache.org/dist/hadoop/common/hadoop-3.3.6/hadoop-3.3.6.tar.gz

# Extract Hadoop
tar -xzf hadoop-3.3.6.tar.gz

# Move to installation directory
sudo mv hadoop-3.3.6 /opt/hadoop

# Set ownership (Linux)
sudo chown -R $USER:$USER /opt/hadoop
```

#### Step 4: Configure Environment Variables

Add to `~/.bashrc` or `~/.zshrc`:

```bash
# Hadoop environment variables
export HADOOP_HOME=/opt/hadoop
export HADOOP_CONF_DIR=$HADOOP_HOME/etc/hadoop
export PATH=$PATH:$HADOOP_HOME/bin:$HADOOP_HOME/sbin

# Hadoop classpath
export HADOOP_CLASSPATH=$HADOOP_HOME/share/hadoop/tools/lib/*

# Java options for Hadoop
export HADOOP_OPTS="$HADOOP_OPTS -Djava.library.path=$HADOOP_HOME/lib/native"

# Source the updated profile
source ~/.bashrc  # or source ~/.zshrc
```

#### Step 5: Configure Hadoop

Edit the following configuration files in `$HADOOP_HOME/etc/hadoop/`:

**1. core-site.xml**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>fs.defaultFS</name>
        <value>hdfs://localhost:9000</value>
        <description>NameNode URI</description>
    </property>
    <property>
        <name>hadoop.tmp.dir</name>
        <value>/tmp/hadoop-${user.name}</value>
        <description>Base temporary directory</description>
    </property>
</configuration>
```

**2. hdfs-site.xml**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>dfs.replication</name>
        <value>1</value>
        <description>Block replication factor</description>
    </property>
    <property>
        <name>dfs.namenode.name.dir</name>
        <value>file:///tmp/hadoop-${user.name}/dfs/name</value>
        <description>NameNode metadata directory</description>
    </property>
    <property>
        <name>dfs.datanode.data.dir</name>
        <value>file:///tmp/hadoop-${user.name}/dfs/data</value>
        <description>DataNode data directory</description>
    </property>
    <property>
        <name>dfs.webhdfs.enabled</name>
        <value>true</value>
        <description>Enable WebHDFS</description>
    </property>
</configuration>
```

**3. mapred-site.xml**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>mapreduce.framework.name</name>
        <value>yarn</value>
        <description>MapReduce framework</description>
    </property>
    <property>
        <name>mapreduce.application.classpath</name>
        <value>$HADOOP_MAPRED_HOME/share/hadoop/mapreduce/*:$HADOOP_MAPRED_HOME/share/hadoop/mapreduce/lib/*</value>
        <description>MapReduce classpath</description>
    </property>
</configuration>
```

**4. yarn-site.xml**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>yarn.nodemanager.aux-services</name>
        <value>mapreduce_shuffle</value>
        <description>Auxiliary service for MapReduce</description>
    </property>
    <property>
        <name>yarn.nodemanager.env-whitelist</name>
        <value>JAVA_HOME,HADOOP_COMMON_HOME,HADOOP_HDFS_HOME,HADOOP_CONF_DIR,CLASSPATH_PREPEND_DISTCACHE,HADOOP_YARN_HOME,HADOOP_MAPRED_HOME</value>
        <description>Environment whitelist for NodeManager</description>
    </property>
</configuration>
```

#### Step 6: Setup SSH for Password-less Login

```bash
# Install SSH server (if not already installed)
sudo apt install openssh-server  # Ubuntu/Debian

# Generate SSH keys
ssh-keygen -t rsa -P '' -f ~/.ssh/id_rsa

# Add public key to authorized keys
cat ~/.ssh/id_rsa.pub >> ~/.ssh/authorized_keys

# Set proper permissions
chmod 0600 ~/.ssh/authorized_keys
chmod 0700 ~/.ssh

# Test SSH localhost
ssh localhost
```

#### Step 7: Format NameNode

```bash
# Format the NameNode (run only once)
hdfs namenode -format

# If you see "Storage directory ... has been successfully formatted"
# then the NameNode has been formatted successfully
```

## Starting Hadoop

### Using the Automated Scripts

If you used the automated installation, you can use the convenience scripts:

```bash
# Start Hadoop cluster
./start_hadoop.sh

# Stop Hadoop cluster
./stop_hadoop.sh

# Test installation
./test_hadoop.sh
```

### Manual Startup

#### Step 1: Start HDFS

```bash
# Start HDFS daemons
start-dfs.sh

# Check HDFS processes
jps
# You should see: NameNode, DataNode, SecondaryNameNode
```

#### Step 2: Start YARN

```bash
# Start YARN daemons
start-yarn.sh

# Check YARN processes
jps
# You should see: NodeManager, ResourceManager (in addition to HDFS processes)
```

#### Step 3: Verify Services

```bash
# Check all Java processes
jps

# You should see approximately:
# 12345 NameNode
# 12346 DataNode
# 12347 SecondaryNameNode
# 12348 NodeManager
# 12349 ResourceManager
```

## Web Interfaces

Once Hadoop is running, you can access the web interfaces:

### HDFS Web UI
- **URL**: http://localhost:9870
- **Description**: HDFS cluster management, file browser
- **Features**: 
  - Browse HDFS file system
  - View cluster status
  - Monitor NameNode and DataNode

### YARN Web UI
- **URL**: http://localhost:8088
- **Description**: YARN cluster management, application tracking
- **Features**:
  - Monitor running applications
  - View resource usage
  - Submit new MapReduce jobs

### MapReduce Job History Server
- **URL**: http://localhost:19888
- **Description**: MapReduce job history and logs

## Basic Hadoop Operations

### HDFS Operations

```bash
# List files in HDFS
hdfs dfs -ls /

# Create a directory in HDFS
hdfs dfs -mkdir /user

# Create a directory with your username
hdfs dfs -mkdir /user/$USER

# Upload a file to HDFS
hdfs dfs -put localfile.txt /user/$USER/

# Download a file from HDFS
hdfs dfs -get /user/$USER/remotefile.txt localfile.txt

# View file contents
hdfs dfs -cat /user/$USER/filename.txt

# Remove files/directories
hdfs dfs -rm /user/$USER/filename.txt

# Get file statistics
hdfs dfs -ls -R /
```

### MapReduce Examples

```bash
# Run Word Count example
hadoop jar $HADOOP_HOME/share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.6.jar wordcount input.txt output

# Run Pi estimation
hadoop jar $HADOOP_HOME/share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.6.jar pi 16 1000

# Run TeraSort benchmark
hadoop jar $HADOOP_HOME/share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.6.jar terasort /input /output
```

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: "Java is not installed or JAVA_HOME is not set"

**Solution:**
```bash
# Install Java
sudo apt install openjdk-8-jdk

# Set JAVA_HOME
export JAVA_HOME=/usr/lib/jvm/java-8-openjdk-amd64
```

#### Issue 2: "ssh: connect to host localhost port 22: Connection refused"

**Solution:**
```bash
# Install and start SSH server
sudo apt install openssh-server
sudo service ssh start

# Enable SSH service to start on boot
sudo systemctl enable ssh
```

#### Issue 3: "NameNode is not formatted"

**Solution:**
```bash
# Format NameNode
hdfs namenode -format

# WARNING: This will destroy all existing data in HDFS
```

#### Issue 4: "Permission denied" errors

**Solution:**
```bash
# Set proper permissions on Hadoop directories
chmod -R 755 $HADOOP_HOME
chmod -R 755 /tmp/hadoop-$USER

# For HDFS data directories
chown -R $USER:$USER /tmp/hadoop-$USER
```

#### Issue 5: "OutOfMemoryError: Java heap space"

**Solution:**
Edit `$HADOOP_HOME/etc/hadoop/hadoop-env.sh`:

```bash
export HADOOP_HEAPSIZE=1024  # Set to 1GB
```

#### Issue 6: Web UIs not accessible

**Solution:**
```bash
# Check if services are running
jps

# Check firewall rules
sudo ufw status  # Ubuntu/Debian
sudo firewall-cmd --list-all  # CentOS/RHEL

# Ensure ports 9870, 8088, 19888 are open
```

### Debug Commands

```bash
# Check Hadoop logs
tail -f $HADOOP_HOME/logs/hadoop-*.log

# Check specific daemon logs
tail -f $HADOOP_HOME/logs/hadoop-*-namenode-*.log
tail -f $HADOOP_HOME/logs/hadoop-*-datanode-*.log

# Check environment variables
echo $JAVA_HOME
echo $HADOOP_HOME

# Test Java installation
java -version
$HADOOP_HOME/bin/hadoop version

# Check network connectivity
netstat -tulpn | grep :9000
netstat -tulpn | grep :8088
```

## Performance Tuning

### For Development (Small Datasets)

Use the configurations provided in this guide. They are optimized for learning and development.

### For Production (Large Datasets)

Consider these optimizations:

1. **Increase heap sizes**:
   ```bash
   export HADOOP_HEAPSIZE=4096  # 4GB
   ```

2. **Configure proper data directories**:
   ```xml
   <property>
       <name>dfs.datanode.data.dir</name>
       <value>file:///data1/hadoop/datanode,file:///data2/hadoop/datanode</value>
   </property>
   ```

3. **Optimize for your hardware**:
   - Use multiple disks for data storage
   - Configure appropriate block sizes
   - Set appropriate replication factors

## Uninstallation

To completely remove Hadoop:

```bash
# Stop Hadoop services
stop-yarn.sh
stop-dfs.sh

# Remove Hadoop directory
rm -rf /opt/hadoop  # or wherever Hadoop was installed

# Remove environment variables from ~/.bashrc

# Remove Hadoop data directories
rm -rf /tmp/hadoop-$USER

# Remove SSH keys (optional)
rm -rf ~/.ssh/id_rsa*
```

## Learning Resources

### Official Documentation
- [Apache Hadoop Documentation](https://hadoop.apache.org/docs/)
- [HDFS User Guide](https://hadoop.apache.org/docs/current/hadoop-project-dist/hadoop-hdfs/HdfsUserGuide.html)
- [YARN Architecture](https://hadoop.apache.org/docs/current/hadoop-yarn/hadoop-yarn-site/YARN.html)

### Tutorial Resources
- [Hadoop Tutorial by Google Cloud](https://cloud.google.com/storage/docs/hadoop-configurations)
- [Hadoop Tutorial by AWS](https://aws.amazon.com/emr/latest/GettingStartedGuide/emr-getting-started-intro.html)

### Practice Exercises
1. Upload sample data to HDFS
2. Run Word Count on your own text files
3. Create your own MapReduce jobs
4. Explore the web UIs and monitor jobs

## Course Assignments

### Assignment 1: Basic HDFS Operations
1. Create a directory structure in HDFS
2. Upload at least 3 different file types
3. Experiment with HDFS commands
4. Document your findings

### Assignment 2: MapReduce Programming
1. Write a simple MapReduce job
2. Process a real dataset
3. Monitor job execution via web UI
4. Analyze results

### Assignment 3: YARN Resource Management
1. Submit multiple jobs simultaneously
2. Monitor resource allocation
3. Experiment with different queue configurations
4. Document performance observations

---

**Need Help?**
- Check the troubleshooting section above
- Review Hadoop logs in `$HADOOP_HOME/logs/`
- Consult the official Hadoop documentation
- Reach out to your course instructor

**Happy Learning with Hadoop! 🚀**