import sys

file_path = "DatabaseCheckController.cs"
with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

stack = []
for i, line in enumerate(lines):
    for char in line:
        if char == "{":
            stack.append(i + 1)
        elif char == "}":
            if not stack:
                print(f"Extra closing brace at line {i+1}")
            else:
                start_line = stack.pop()
                # print(f"Block from {start_line} to {i+1}")

if stack:
    for start_line in stack:
        print(f"Unclosed brace starting at line {start_line}")
else:
    print("All braces are balanced.")
