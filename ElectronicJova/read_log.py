
try:
    with open('build_utf8.log', 'r', encoding='utf-8') as f:
        lines = f.readlines()
        print(''.join(lines[-50:]))
except Exception as e:
    print(f"Error reading file: {e}")
