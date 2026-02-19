import sys
try:
    sys.stdout.reconfigure(encoding='utf-8')
    with open('build_log_2.txt', 'r', encoding='utf-16') as f:
        for line in f:
            if "error" in line.lower():
                print(line.strip())
except Exception as e:
    print(e)
