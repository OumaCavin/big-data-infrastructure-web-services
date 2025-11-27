#!/usr/bin/env python3
"""
Hadoop Installation and Setup Script for SDS 6104 Course

This script automates the installation and configuration of Hadoop 3.3.6
for educational purposes in the Big Data Infrastructure course.

Author: Cavin Otieno
Course: SDS 6104 - Big Data Infrastructure, Platforms and Warehousing
Date: 2025-11-27
"""

import os
import sys
import subprocess
import platform
import urllib.request
import tarfile
import json
import socket
import shutil
from pathlib import Path
from typing import Dict, List, Tuple, Optional

class HadoopInstaller:
    """Automated Hadoop installer and configurator"""
    
    def __init__(self):
        self.system = platform.system().lower()
        self.architecture = platform.machine().lower()
        self.home_dir = Path.home()
        self.workspace_dir = Path.cwd()
        self.hadoop_version = "3.3.6"
        self.java_home = ""
        self.hadoop_home = ""
        
    def run_command(self, command: str, check: bool = True) -> Tuple[int, str, str]:
        """Run system command and return exit code, stdout, stderr"""
        try:
            result = subprocess.run(
                command, 
                shell=True, 
                capture_output=True, 
                text=True, 
                check=check
            )
            return result.returncode, result.stdout, result.stderr
        except subprocess.CalledProcessError as e:
            return e.returncode, e.stdout, e.stderr
    
    def check_prerequisites(self) -> bool:
        """Check system prerequisites for Hadoop installation"""
        print("🔍 Checking system prerequisites...")
        
        # Check Python version
        python_version = sys.version_info
        if python_version.major < 3 or (python_version.major == 3 and python_version.minor < 7):
            print("❌ Python 3.7+ required")
            return False
        print(f"✅ Python {python_version.major}.{python_version.minor}.{python_version.micro}")
        
        # Check Java installation
        java_status, java_out, java_err = self.run_command("java -version", check=False)
        if java_status != 0:
            print("⚠️  Java not found. Installing OpenJDK 8...")
            if not self.install_java():
                return False
        else:
            java_version_line = java_out.split('\n')[0] if java_out else "Java not detected"
            print(f"✅ {java_version_line}")
        
        # Set JAVA_HOME
        self.java_home = self.detect_java_home()
        if not self.java_home:
            print("❌ Could not detect JAVA_HOME")
            return False
        
        # Check available disk space (minimum 5GB)
        disk_space = shutil.disk_usage(self.workspace_dir).free / (1024**3)
        if disk_space < 5:
            print(f"❌ Insufficient disk space: {disk_space:.1f}GB (minimum 5GB required)")
            return False
        print(f"✅ Available disk space: {disk_space:.1f}GB")
        
        # Check network connectivity
        if not self.check_internet():
            print("❌ No internet connectivity")
            return False
        print("✅ Internet connectivity confirmed")
        
        return True
    
    def install_java(self) -> bool:
        """Install Java Development Kit"""
        print("☕ Installing OpenJDK 8...")
        
        try:
            if self.system == "linux":
                # Try apt-based installation
                commands = [
                    "sudo apt update",
                    "sudo apt install -y openjdk-8-jdk"
                ]
                
                for cmd in commands:
                    status, _, stderr = self.run_command(cmd)
                    if status != 0:
                        print(f"⚠️  Failed to install Java via apt: {stderr}")
                        return False
                        
            elif self.system == "darwin":  # macOS
                status, _, stderr = self.run_command("brew install openjdk@8")
                if status != 0:
                    print(f"⚠️  Failed to install Java via Homebrew: {stderr}")
                    return False
                    
            elif self.system == "windows":
                print("❌ Windows Java installation not automated. Please install OpenJDK 8 manually.")
                return False
            
            # Update JAVA_HOME after installation
            self.java_home = self.detect_java_home()
            return True
            
        except Exception as e:
            print(f"❌ Error installing Java: {e}")
            return False
    
    def detect_java_home(self) -> str:
        """Detect JAVA_HOME environment variable"""
        # Try common Java installation paths
        common_paths = [
            "/usr/lib/jvm/java-8-openjdk-amd64",
            "/usr/lib/jvm/java-8-openjdk-arm64",
            "/opt/java8",
            "/Library/Java/JavaVirtualMachines/adoptopenjdk-8.jdk/Contents/Home",
            "/usr/local/opt/openjdk@8"
        ]
        
        for path in common_paths:
            if os.path.exists(os.path.join(path, "bin", "java")):
                return path
        
        # Try to get from environment
        env_java_home = os.environ.get("JAVA_HOME")
        if env_java_home and os.path.exists(os.path.join(env_java_home, "bin", "java")):
            return env_java_home
        
        # Try to detect from java command
        status, java_path, _ = self.run_command("which java", check=False)
        if status == 0 and java_path.strip():
            java_bin_path = java_path.strip()
            # Try to get real path
            status, real_path, _ = self.run_command(f"readlink -f {java_bin_path}", check=False)
            if status == 0:
                java_bin = real_path.strip()
                java_home = os.path.dirname(os.path.dirname(java_bin))
                if os.path.exists(java_home):
                    return java_home
        
        return ""
    
    def check_internet(self) -> bool:
        """Check internet connectivity"""
        try:
            socket.create_connection(("8.8.8.8", 53), timeout=3)
            return True
        except OSError:
            return False
    
    def download_hadoop(self) -> bool:
        """Download Hadoop distribution"""
        print("📦 Downloading Hadoop...")
        
        # Determine architecture and appropriate package
        if "x86_64" in self.architecture or "amd64" in self.architecture:
            package_name = f"hadoop-{self.hadoop_version}.tar.gz"
        else:
            print(f"⚠️  Architecture {self.architecture} may not be supported")
            package_name = f"hadoop-{self.hadoop_version}.tar.gz"
        
        # Hadoop download URLs
        base_url = "https://archive.apache.org/dist/hadoop/common"
        download_url = f"{base_url}/hadoop-{self.hadoop_version}/{package_name}"
        download_path = self.workspace_dir / package_name
        
        try:
            print(f"📥 Downloading from: {download_url}")
            
            # Download with progress
            def download_progress(block_num, block_size, total_size):
                downloaded = block_num * block_size
                percentage = (downloaded / total_size) * 100 if total_size > 0 else 0
                print(f"\r📥 Progress: {percentage:.1f}% ({downloaded/1024/1024:.1f}MB)", end="")
            
            urllib.request.urlretrieve(download_url, download_path, download_progress)
            print(f"\n✅ Downloaded {package_name}")
            
            # Verify download
            expected_size = 350 * 1024 * 1024  # ~350MB
            actual_size = download_path.stat().st_size
            if actual_size < expected_size * 0.9:
                print(f"❌ Download appears incomplete: {actual_size/1024/1024:.1f}MB")
                return False
            
            print(f"✅ Download verified: {actual_size/1024/1024:.1f}MB")
            return True
            
        except Exception as e:
            print(f"❌ Download failed: {e}")
            return False
    
    def extract_hadoop(self) -> bool:
        """Extract Hadoop distribution"""
        print("📂 Extracting Hadoop...")
        
        package_name = f"hadoop-{self.hadoop_version}.tar.gz"
        extract_path = self.workspace_dir / package_name.replace('.tar.gz', '')
        
        try:
            with tarfile.open(self.workspace_dir / package_name, 'r:gz') as tar:
                tar.extractall(self.workspace_dir)
            
            print(f"✅ Hadoop extracted to: {extract_path}")
            self.hadoop_home = str(extract_path)
            return True
            
        except Exception as e:
            print(f"❌ Extraction failed: {e}")
            return False
    
    def configure_hadoop(self) -> bool:
        """Configure Hadoop for pseudo-distributed operation"""
        print("⚙️  Configuring Hadoop...")
        
        try:
            hadoop_conf_dir = Path(self.hadoop_home) / "etc" / "hadoop"
            
            # Create configuration files
            configs = self.create_hadoop_configs()
            
            for filename, content in configs.items():
                config_file = hadoop_conf_dir / filename
                with open(config_file, 'w') as f:
                    f.write(content)
                print(f"✅ Created {filename}")
            
            # Set execute permissions for Hadoop scripts
            bin_dir = Path(self.hadoop_home) / "bin"
            sbin_dir = Path(self.hadoop_home) / "sbin"
            
            for script_dir in [bin_dir, sbin_dir]:
                for script in script_dir.glob("*"):
                    if script.is_file():
                        script.chmod(0o755)
            
            print("✅ Hadoop configuration completed")
            return True
            
        except Exception as e:
            print(f"❌ Configuration failed: {e}")
            return False
    
    def create_hadoop_configs(self) -> Dict[str, str]:
        """Create Hadoop configuration files"""
        return {
            "core-site.xml": f"""<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>fs.defaultFS</name>
        <value>hdfs://localhost:9000</value>
        <description>NameNode URI</description>
    </property>
    <property>
        <name>hadoop.tmp.dir</name>
        <value>{self.workspace_dir / 'hadoop-tmp'}</value>
        <description>Base temporary directory</description>
    </property>
</configuration>""",
            
            "hdfs-site.xml": f"""<?xml version="1.0" encoding="UTF-8"?>
<?xml-stylesheet type="text/xsl" href="configuration.xsl"?>
<configuration>
    <property>
        <name>dfs.replication</name>
        <value>1</value>
        <description>Block replication factor</description>
    </property>
    <property>
        <name>dfs.namenode.name.dir</name>
        <value>file://{self.workspace_dir / 'hadoop-data' / 'namenode'}</value>
        <description>NameNode metadata directory</description>
    </property>
    <property>
        <name>dfs.datanode.data.dir</name>
        <value>file://{self.workspace_dir / 'hadoop-data' / 'datanode'}</value>
        <description>DataNode data directory</description>
    </property>
    <property>
        <name>dfs.webhdfs.enabled</name>
        <value>true</value>
        <description>Enable WebHDFS</description>
    </property>
</configuration>""",
            
            "mapred-site.xml": """<?xml version="1.0" encoding="UTF-8"?>
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
</configuration>""",
            
            "yarn-site.xml": """<?xml version="1.0" encoding="UTF-8"?>
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
</configuration>"""
        }
    
    def setup_environment(self) -> bool:
        """Setup environment variables and paths"""
        print("🔧 Setting up environment...")
        
        try:
            # Create Hadoop data directories
            data_dirs = [
                self.workspace_dir / 'hadoop-data' / 'namenode',
                self.workspace_dir / 'hadoop-data' / 'datanode',
                self.workspace_dir / 'hadoop-tmp'
            ]
            
            for dir_path in data_dirs:
                dir_path.mkdir(parents=True, exist_ok=True)
            
            # Create environment setup script
            env_script = f"""#!/bin/bash
# Hadoop Environment Setup for SDS 6104 Course

export JAVA_HOME="{self.java_home}"
export HADOOP_HOME="{self.hadoop_home}"
export HADOOP_CONF_DIR="$HADOOP_HOME/etc/hadoop"
export PATH="$PATH:$HADOOP_HOME/bin:$HADOOP_HOME/sbin"
export PATH="$PATH:$HADOOP_HOME/lib/native"

# Hadoop classpath
export HADOOP_CLASSPATH=$HADOOP_HOME/share/hadoop/tools/lib/*

# Java options
export HADOOP_OPTS="$HADOOP_OPTS -Djava.library.path=$HADOOP_HOME/lib/native"

echo "✅ Hadoop environment configured"
echo "JAVA_HOME: $JAVA_HOME"
echo "HADOOP_HOME: $HADOOP_HOME"
echo ""
echo "To activate this environment, run: source hadoop_env.sh"
"""
            
            env_script_path = self.workspace_dir / 'hadoop_env.sh'
            with open(env_script_path, 'w') as f:
                f.write(env_script)
            
            # Make script executable
            os.chmod(env_script_path, 0o755)
            
            print(f"✅ Environment script created: {env_script_path}")
            return True
            
        except Exception as e:
            print(f"❌ Environment setup failed: {e}")
            return False
    
    def test_installation(self) -> bool:
        """Test Hadoop installation"""
        print("🧪 Testing Hadoop installation...")
        
        try:
            # Source environment
            env_script = f"source {self.workspace_dir}/hadoop_env.sh &&"
            
            # Test Hadoop version command
            status, output, error = self.run_command(
                f"{env_script} hadoop version", 
                check=False
            )
            
            if status == 0:
                print("✅ Hadoop version command successful")
                version_lines = output.strip().split('\n')
                for line in version_lines[:3]:  # Show first 3 lines
                    print(f"   {line}")
            else:
                print(f"❌ Hadoop version command failed: {error}")
                return False
            
            return True
            
        except Exception as e:
            print(f"❌ Installation test failed: {e}")
            return False
    
    def create_startup_scripts(self) -> bool:
        """Create convenient startup scripts"""
        print("📝 Creating startup scripts...")
        
        try:
            # Start Hadoop script
            start_script = f"""#!/bin/bash
# Start Hadoop Services

echo "🚀 Starting Hadoop cluster..."

# Source environment
source {self.workspace_dir}/hadoop_env.sh

# Format namenode (if not already formatted)
if [ ! -f "$HADOOP_DATA_DIR/namenode/current/VERSION" ]; then
    echo "📋 Formatting NameNode..."
    hdfs namenode -format -force
fi

# Start HDFS
echo "🗂️  Starting HDFS..."
start-dfs.sh

# Start YARN
echo "📊 Starting YARN..."
start-yarn.sh

# Wait a bit for services to start
sleep 5

# Check status
echo "📊 Checking cluster status..."
jps

echo ""
echo "✅ Hadoop cluster started!"
echo "🌐 HDFS Web UI: http://localhost:9870"
echo "📊 YARN Web UI: http://localhost:8088"
echo ""
echo "To stop the cluster, run: ./stop_hadoop.sh"
"""
            
            # Stop Hadoop script
            stop_script = """#!/bin/bash
# Stop Hadoop Services

echo "🛑 Stopping Hadoop cluster..."

# Source environment
source ./hadoop_env.sh

# Stop YARN
echo "📊 Stopping YARN..."
stop-yarn.sh

# Stop HDFS
echo "🗂️  Stopping HDFS..."
stop-dfs.sh

# Show final status
echo ""
echo "📊 Final process status:"
jps

echo ""
echo "✅ Hadoop cluster stopped!"
"""
            
            # Test script
            test_script = f"""#!/bin/bash
# Test Hadoop Installation

echo "🧪 Testing Hadoop installation..."

# Source environment
source {self.workspace_dir}/hadoop_env.sh

echo "1. Testing Hadoop version..."
hadoop version

echo ""
echo "2. Testing HDFS commands..."
hdfs dfs -ls /

echo ""
echo "3. Testing MapReduce example..."
hadoop jar $HADOOP_HOME/share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.6.jar pi 2 5

echo ""
echo "✅ Hadoop test completed!"
"""
            
            # Write scripts
            scripts = [
                ("start_hadoop.sh", start_script),
                ("stop_hadoop.sh", stop_script),
                ("test_hadoop.sh", test_script)
            ]
            
            for script_name, script_content in scripts:
                script_path = self.workspace_dir / script_name
                with open(script_path, 'w') as f:
                    f.write(script_content)
                os.chmod(script_path, 0o755)
                print(f"✅ Created {script_name}")
            
            return True
            
        except Exception as e:
            print(f"❌ Startup scripts creation failed: {e}")
            return False
    
    def install(self) -> bool:
        """Main installation process"""
        print("=" * 60)
        print("🐘 Hadoop Installation for SDS 6104 Course")
        print("=" * 60)
        
        steps = [
            ("Checking prerequisites", self.check_prerequisites),
            ("Downloading Hadoop", self.download_hadoop),
            ("Extracting Hadoop", self.extract_hadoop),
            ("Configuring Hadoop", self.configure_hadoop),
            ("Setting up environment", self.setup_environment),
            ("Testing installation", self.test_installation),
            ("Creating startup scripts", self.create_startup_scripts)
        ]
        
        for step_name, step_func in steps:
            print(f"\n🔄 {step_name}...")
            
            if not step_func():
                print(f"❌ {step_name} failed!")
                return False
        
        print("\n" + "=" * 60)
        print("🎉 Hadoop installation completed successfully!")
        print("=" * 60)
        
        print("\n📋 Next Steps:")
        print("1. Source the environment: source hadoop_env.sh")
        print("2. Start Hadoop: ./start_hadoop.sh")
        print("3. Test installation: ./test_hadoop.sh")
        print("4. Access web UIs:")
        print("   - HDFS: http://localhost:9870")
        print("   - YARN: http://localhost:8088")
        
        print("\n📚 Learning Resources:")
        print("- Hadoop Documentation: https://hadoop.apache.org/docs/")
        print("- HDFS Commands: hdfs dfs -help")
        print("- MapReduce Examples: hadoop jar $HADOOP_HOME/share/hadoop/mapreduce/hadoop-mapreduce-examples-*.jar")
        
        return True

def main():
    """Main entry point"""
    installer = HadoopInstaller()
    
    try:
        success = installer.install()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⚠️  Installation cancelled by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()