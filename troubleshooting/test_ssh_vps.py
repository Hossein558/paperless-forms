import paramiko
import traceback

host = '91.107.251.112'
port = 1530
user = 'root'
password = '135246Eac'

try:
    print(f"Connecting to {host}:{port} as {user}...")
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(host, port=port, username=user, password=password, timeout=10)
    print("Successfully connected to VPS!")
    
    # Run a simple command
    stdin, stdout, stderr = ssh.exec_command('uname -a')
    print("Server info:", stdout.read().decode('utf-8').strip())
    
    # Test SFTP write and read
    print("Testing SFTP (writing and downloading a test file)...")
    sftp = ssh.open_sftp()
    
    test_content = "This is a test file from the agent."
    remote_path = "/root/test_file.txt"
    local_path = "test_file_downloaded.txt"
    
    # Write to VPS
    with sftp.file(remote_path, 'w') as f:
        f.write(test_content)
    print(f"Successfully wrote to {remote_path} on VPS.")
    
    # Download from VPS
    sftp.get(remote_path, local_path)
    print(f"Successfully downloaded to local Windows system as {local_path}.")
    
    # Clean up
    sftp.remove(remote_path)
    sftp.close()
    ssh.close()
    print("Test 1 (VPS <-> Local) completed successfully!")
    
except Exception as e:
    print("Failed to connect or execute:")
    traceback.print_exc()
