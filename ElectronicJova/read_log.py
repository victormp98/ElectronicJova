try:
    with open('build_log.txt', 'r', encoding='utf-16') as f:
        print(f.read())
except Exception as e:
    print(e)
