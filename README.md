> Update: 07.09.2026

:warning: breaking change, delimiter changed from | to ¦

### How to use
+ Drag and drop js file for translations on exe

### How to get file for translation
1. Create folder on c:/
2. Open Whatchface Tool
3. F12 -> Source -> Override + link created folder
4. Network (press F5) -> index-xxxxxxxx.js -> Save for overrides
5. Go to folder, and edit created file

https://github.com/user-attachments/assets/183152f3-2156-4ddd-b3a0-cbdf1a041ba8

### Smart string replace

String with dynamic vars to change:
```
msg:`不能超过${d}个字符`
```

config:
```
⚡msg:`不能超过#0#个字符`¦msg:`No more than #0# characters`
```

Rules:
1) Line must start with ⚡ for advanced mode.
2) Keep the beginning and end of the string unchanged.
3) Placeholders #0#, #1#, #2#... are supported.

> password:123
