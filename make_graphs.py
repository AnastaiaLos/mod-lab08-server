import pandas as pd
import matplotlib.pyplot as plt

# Читаем данные из analysis.txt (разделитель табуляции)
df = pd.read_csv('result/analysis.txt', sep='\t')

# Определяем, какие колонки есть
# λ, P0_теор, P_отк_теор, Q_теор, A_теор, k_теор, P_отк_эксп, Q_эксп, A_эксп, k_эксп

# Список графиков: (имя_файла, заголовок, колонка_теор, колонка_эксп, единица_измерения)
plots = [
    ('p-1.png', 'Вероятность простоя системы P0', 'P0_теор', 'P0_эксп', 'P0'),
    ('p-2.png', 'Вероятность отказа Pотк', 'P_отк_теор', 'P_отк_эксп', 'Pотк'),
    ('p-3.png', 'Относительная пропускная способность Q', 'Q_теор', 'Q_эксп', 'Q'),
    ('p-4.png', 'Абсолютная пропускная способность A', 'A_теор', 'A_эксп', 'A'),
    ('p-5.png', 'Среднее число занятых каналов k', 'k_теор', 'k_эксп', 'k')
]

for filename, title, col_theor, col_exp, ylabel in plots:
    plt.figure(figsize=(8,5))
    plt.plot(df['λ'], df[col_theor], 'bo-', label='Теория')
    plt.plot(df['λ'], df[col_exp], 'ro--', label='Эксперимент')
    plt.xlabel('Интенсивность входного потока λ')
    plt.ylabel(ylabel)
    plt.title(title)
    plt.legend()
    plt.grid(True)
    plt.savefig(f'result/{filename}', dpi=150)
    plt.close()
    print(f'Создан {filename}')
