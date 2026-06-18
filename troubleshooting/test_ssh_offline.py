import paramiko
import traceback
import os

host = '172.25.0.42'
port = 22
user = 'root'
password = '135246Eac'

try:
    print(f"Connecting to {host}:{port} as {user}...")
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(host, port=port, username=user, password=password, timeout=10)
    print("Successfully connected to Jira Offline Server (0.42)!")
    
    # Run a simple command
    stdin, stdout, stderr = ssh.exec_command('uname -a && mkdir -p /home/jira')
    print("Server info:", stdout.read().decode('utf-8').strip())
    
    # Test SFTP upload
    print("Testing SFTP (Uploading a test file from local Windows to offline server)...")
    sftp = ssh.open_sftp()
    
    test_content = "This is a test upload from the Windows machine to the offline Jira server."
    local_path = "test_upload.txt"
    remote_path = "/home/jira/test_upload_from_agent.txt"
    
    # Create local file
    with open(local_path, "w", encoding="utf-8") as f:
        f.write(test_content)
        
    # Upload to Server 0.42
    sftp.put(local_path, remote_path)
    print(f"Successfully uploaded to {remote_path} on Offline Server (0.42).")
    
    sftp.close()
    ssh.close()
    print("Test 2 (Local -> Offline 0.42) completed successfully!")
    
except Exception as e:
    print("Failed to connect or execute:")
    traceback.print_exc()
