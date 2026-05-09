import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyArrowPatch, FancyBboxPatch
import os

OUT = r'C:\Git\ETL-wpf-simulator-web-project\diploma'

# ── кольори ──────────────────────────────────────────────────────────────────
C_BLUE   = '#2563EB'
C_LBLUE  = '#DBEAFE'
C_GREEN  = '#15803D'
C_LGREEN = '#DCFCE7'
C_GRAY   = '#374151'
C_LGRAY  = '#F3F4F6'
C_BRONZE = '#92400E'
C_LBRONZE= '#FEF3C7'
C_SILVER = '#374151'
C_LSILV  = '#E5E7EB'
C_GOLD   = '#92400E'
C_LGOLD  = '#FEF9C3'
C_RED    = '#991B1B'
C_LRED   = '#FEE2E2'
C_PURPLE = '#6D28D9'
C_LPURP  = '#EDE9FE'

def box(ax, x, y, w, h, label, sublabel='', fc='#DBEAFE', ec='#2563EB', lw=1.5,
        fontsize=10, subfontsize=8, bold=False):
    rect = FancyBboxPatch((x, y), w, h,
                          boxstyle='round,pad=0.02',
                          facecolor=fc, edgecolor=ec, linewidth=lw)
    ax.add_patch(rect)
    weight = 'bold' if bold else 'normal'
    ax.text(x + w/2, y + h/2 + (0.08 if sublabel else 0),
            label, ha='center', va='center',
            fontsize=fontsize, fontweight=weight, color=ec)
    if sublabel:
        ax.text(x + w/2, y + h/2 - 0.12,
                sublabel, ha='center', va='center',
                fontsize=subfontsize, color='#6B7280', style='italic')

def arrow(ax, x1, y1, x2, y2, color='#374151', lw=1.5, label='', ls='-'):
    ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle='->', color=color,
                                lw=lw, linestyle=ls))
    if label:
        mx, my = (x1+x2)/2, (y1+y2)/2
        ax.text(mx+0.04, my, label, fontsize=7.5, color=color,
                ha='left', va='center',
                bbox=dict(fc='white', ec='none', pad=1))

def line(ax, x1, y1, x2, y2, color='#9CA3AF', lw=1.0, ls='--'):
    ax.plot([x1, x2], [y1, y2], color=color, lw=lw, ls=ls)

# ════════════════════════════════════════════════════════════════════════════
# Рис 3.1 — Загальна архітектура рішення
# ════════════════════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(13, 7))
ax.set_xlim(0, 13); ax.set_ylim(0, 7)
ax.axis('off')
ax.set_facecolor('white')
fig.patch.set_facecolor('white')

ax.text(6.5, 6.6, 'Архітектура рішення', ha='center', va='center',
        fontsize=14, fontweight='bold', color=C_GRAY)

# WPF box
box(ax, 0.4, 2.8, 3.6, 3.2, 'ETL_simulator', '(WPF-застосунок)',
    fc='#EFF6FF', ec=C_BLUE, lw=2, fontsize=11, bold=True)
for i, (lbl, fc, ec) in enumerate([
    ('SalesGenerator\n(Bogus)', C_LBRONZE, C_BRONZE),
    ('BronzeLoader',             C_LBRONZE, C_BRONZE),
    ('SilverProcessor',          C_LSILV,   C_SILVER),
    ('GoldLoader',               C_LGOLD,   '#B45309'),
    ('EtlOrchestrator',          C_LPURP,   C_PURPLE),
]):
    box(ax, 0.6, 2.95 + i*0.55, 3.2, 0.46, lbl,
        fc=fc, ec=ec, fontsize=8)

# Web box
box(ax, 9.0, 2.8, 3.6, 3.2, 'ETL_web_project', '(ASP.NET Core MVC)',
    fc='#F0FDF4', ec=C_GREEN, lw=2, fontsize=11, bold=True)
for i, lbl in enumerate([
    'EtlController', 'DashboardController',
    'EtlLogService', 'EtlJobService', 'Program.cs (DI)'
]):
    box(ax, 9.2, 2.95 + i*0.55, 3.2, 0.46, lbl,
        fc=C_LGREEN, ec=C_GREEN, fontsize=8)

# DB box
box(ax, 4.6, 0.3, 3.8, 2.0, 'EtlDb (SQL Server)',
    'stg | silver | dw | etl | auth',
    fc='#F9FAFB', ec=C_GRAY, lw=2, fontsize=11, bold=True)
for i, (schema, color) in enumerate([
    ('stg.SalesRaw',  '#FEF3C7'),
    ('silver.SalesClean', '#E5E7EB'),
    ('dw.FactSales + Dims', '#FEF9C3'),
    ('etl.EtlRun / EtlLog', C_LPURP),
]):
    box(ax, 4.75 + (i % 2)*1.85, 0.42 + (i//2)*0.72, 1.75, 0.62,
        schema, fc=color, ec=C_GRAY, fontsize=7.5)

# Стрілки WPF → DB
arrow(ax, 2.2, 2.8, 4.8, 2.3, color=C_BLUE, lw=2, label='EF Core\n(запис/читання)')
# Стрілки Web → DB
arrow(ax, 10.8, 2.8, 8.0, 2.3, color=C_GREEN, lw=2, label='EF Core\n(читання)')

# Named Pipe
ax.annotate('', xy=(9.0, 5.2), xytext=(4.0, 5.2),
            arrowprops=dict(arrowstyle='<->', color=C_PURPLE, lw=2))
ax.text(6.5, 5.42, 'Named Pipe\n"etl-wpf-simulator"',
        ha='center', va='center', fontsize=9, color=C_PURPLE,
        bbox=dict(fc=C_LPURP, ec=C_PURPLE, pad=3, boxstyle='round,pad=0.3'))

# ProjectReference
ax.annotate('', xy=(9.0, 4.2), xytext=(4.0, 4.2),
            arrowprops=dict(arrowstyle='->', color='#9CA3AF', lw=1.5, linestyle='dashed'))
ax.text(6.5, 4.38, '<ProjectReference>', ha='center', va='center',
        fontsize=8, color='#6B7280', style='italic')

ax.text(6.5, 0.08, 'Рисунок 3.1 — Загальна архітектура системи',
        ha='center', fontsize=9, color=C_GRAY, style='italic')

plt.tight_layout()
plt.savefig(os.path.join(OUT, 'fig3_1_architecture.png'), dpi=180, bbox_inches='tight')
plt.close()
print('fig3_1 done')

# ════════════════════════════════════════════════════════════════════════════
# Рис 3.2 — Схема бази даних (по схемах)
# ════════════════════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(15, 9))
ax.set_xlim(0, 15); ax.set_ylim(0, 9)
ax.axis('off')
fig.patch.set_facecolor('white')

ax.text(7.5, 8.65, 'Структура бази даних EtlDb',
        ha='center', fontsize=14, fontweight='bold', color=C_GRAY)

def schema_box(ax, x, y, w, h, title, color_fc, color_ec, fields):
    rect = FancyBboxPatch((x, y), w, h, boxstyle='round,pad=0.05',
                          facecolor=color_fc, edgecolor=color_ec, linewidth=2)
    ax.add_patch(rect)
    # header
    header = FancyBboxPatch((x, y+h-0.52), w, 0.52,
                             boxstyle='round,pad=0.02',
                             facecolor=color_ec, edgecolor=color_ec, linewidth=0)
    ax.add_patch(header)
    ax.text(x+w/2, y+h-0.26, title, ha='center', va='center',
            fontsize=9.5, fontweight='bold', color='white')
    for i, f in enumerate(fields):
        key_icon = '🔑 ' if f.startswith('PK') else ('🔗 ' if f.startswith('FK') else '   ')
        text = f.replace('PK ','').replace('FK ','')
        ax.text(x+0.18, y+h-0.85-i*0.38, key_icon+text,
                va='center', fontsize=8, color='#1F2937')
        if i < len(fields)-1:
            ax.plot([x+0.1, x+w-0.1],
                    [y+h-1.05-i*0.38, y+h-1.05-i*0.38],
                    color='#D1D5DB', lw=0.7)

def fk_arrow(ax, x1, y1, x2, y2):
    ax.annotate('', xy=(x2,y2), xytext=(x1,y1),
                arrowprops=dict(arrowstyle='->', color='#6B7280', lw=1.2,
                                connectionstyle='arc3,rad=0.0',
                                linestyle='dashed'))

# ── stg ──────────────────────────────────────────────────────────────────────
schema_box(ax, 0.3, 5.0, 3.2, 3.5, 'stg.SalesRaw',
           '#FFFBEB', '#D97706', [
    'PK Id  int',
    '   SalesTime  datetime?',
    '   StoreCode  nvarchar?',
    '   ProductCode  nvarchar?',
    '   CustomerCode  nvarchar?',
    '   Quantity  int?',
    '   UnitPrice  decimal?',
    '   IsProcessedToSilver  bit',
])

# ── silver ────────────────────────────────────────────────────────────────────
schema_box(ax, 0.3, 0.3, 3.2, 4.4, 'silver.SalesClean',
           '#F9FAFB', '#6B7280', [
    'PK Id  int',
    'FK SourceId → SalesRaw',
    '   SalesTime  datetime',
    '   StoreCode  nvarchar',
    '   ProductCode  nvarchar',
    '   CustomerCode  nvarchar?',
    '   Quantity  int',
    '   UnitPrice  decimal',
    '   TotalAmount  decimal',
    '   IsProcessedToGold  bit',
])

# ── dw ────────────────────────────────────────────────────────────────────────
schema_box(ax, 4.0, 4.0, 3.2, 4.5, 'dw.FactSales',
           '#FEFCE8', '#A16207', [
    'PK FactId  bigint',
    'FK DateKey → DimDate',
    'FK StoreKey → DimStore',
    'FK ProductKey → DimProduct',
    'FK CustomerKey → DimCustomer?',
    '   Quantity  int',
    '   TotalAmount  decimal',
    '   CreatedAt  datetime',
])

schema_box(ax, 4.0, 0.3, 3.2, 3.4, 'dw.DimDate',
           '#FEFCE8', '#A16207', [
    'PK DateKey  int',
    '   Date  date',
    '   Year  int',
    '   Month  tinyint',
    '   MonthName  nvarchar',
    '   DayOfWeek  tinyint',
    '   DayName  nvarchar',
])

schema_box(ax, 7.7, 5.5, 3.0, 3.0, 'dw.DimStore',
           '#FEFCE8', '#A16207', [
    'PK StoreKey  int',
    '   StoreCode  nvarchar',
    '   StoreName  nvarchar',
    '   City  nvarchar',
    '   Country  nvarchar',
])

schema_box(ax, 7.7, 2.0, 3.0, 3.2, 'dw.DimProduct',
           '#FEFCE8', '#A16207', [
    'PK ProductKey  int',
    '   ProductCode  nvarchar',
    '   ProductName  nvarchar',
    '   Category  nvarchar',
    '   UnitPrice  decimal',
])

schema_box(ax, 7.7, 0.3, 3.0, 1.5, 'dw.DimCustomer',
           '#FEFCE8', '#A16207', [
    'PK CustomerKey  int',
    '   CustomerCode  nvarchar',
    '   FullName  nvarchar',
])

# ── etl ────────────────────────────────────────────────────────────────────────
schema_box(ax, 11.3, 6.2, 3.4, 2.3, 'etl.EtlJob',
           '#F5F3FF', '#7C3AED', [
    'PK JobId  int',
    '   JobCode  nvarchar',
    '   JobName  nvarchar',
    '   IsActive  bit',
])
schema_box(ax, 11.3, 3.5, 3.4, 2.5, 'etl.EtlRun',
           '#F5F3FF', '#7C3AED', [
    'PK RunId  bigint',
    'FK JobId → EtlJob',
    '   Status  EtlStatus',
    '   StartTime  datetime',
    '   RowsRead / RowsInserted',
])
schema_box(ax, 11.3, 0.3, 3.4, 2.9, 'etl.EtlLog',
           '#F5F3FF', '#7C3AED', [
    'PK LogId  bigint',
    'FK RunId → EtlRun',
    '   Level  LogLevel',
    '   Message  nvarchar',
    '   LogTime  datetime',
])

# FK стрілки (ключові)
fk_arrow(ax, 1.9, 5.0, 1.9, 4.7)  # SalesRaw → SalesClean (SourceId)
fk_arrow(ax, 5.6, 4.0, 5.6, 3.7)  # FactSales → DimDate
fk_arrow(ax, 6.7, 5.5, 7.7, 6.8)  # FactSales → DimStore
fk_arrow(ax, 6.7, 5.0, 7.7, 3.6)  # FactSales → DimProduct
fk_arrow(ax, 6.7, 4.5, 7.7, 1.05) # FactSales → DimCustomer
fk_arrow(ax, 12.7, 6.2, 12.7, 6.0) # EtlRun → EtlJob (JobId)
fk_arrow(ax, 12.7, 3.5, 12.7, 3.2) # EtlLog → EtlRun

ax.text(7.5, 0.05, 'Рисунок 3.2 — Схема бази даних EtlDb',
        ha='center', fontsize=9, color=C_GRAY, style='italic')

plt.tight_layout()
plt.savefig(os.path.join(OUT, 'fig3_2_database.png'), dpi=180, bbox_inches='tight')
plt.close()
print('fig3_2 done')

# ════════════════════════════════════════════════════════════════════════════
# Рис 3.3 — Послідовність одного кроку ETL
# ════════════════════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(14, 8))
ax.set_xlim(0, 14); ax.set_ylim(0, 8)
ax.axis('off')
fig.patch.set_facecolor('white')

ax.text(7, 7.65, 'Послідовність виконання одного кроку ETL-пайплайну',
        ha='center', fontsize=13, fontweight='bold', color=C_GRAY)

# Учасники (swim lanes labels)
lanes = [
    ('MainViewModel',   0.1,  2.5,  C_BLUE,   C_LBLUE),
    ('BronzeLoader',    2.6,  2.4,  C_BRONZE, C_LBRONZE),
    ('SilverProcessor', 5.1,  2.4,  C_SILVER, C_LSILV),
    ('GoldLoader',      7.6,  2.4,  '#A16207',C_LGOLD),
    ('EtlOrchestrator', 10.1, 2.4, C_PURPLE, C_LPURP),
    ('EtlDb',           12.6, 1.3,  C_GRAY,   C_LGRAY),
]
for (name, lx, lw, ec, fc) in lanes:
    box(ax, lx, 6.9, lw, 0.6, name, fc=fc, ec=ec, fontsize=9, bold=True)
    # вертикальна лінія учасника
    cx = lx + lw/2
    ax.plot([cx, cx], [0.2, 6.9], color=ec, lw=1, ls='--', alpha=0.5)

# Центри учасників
def cx(i): return lanes[i][1] + lanes[i][2]/2

steps = [
    # (y, від, до, текст, колір стрілки)
    (6.4, 0, 1, 'GenerateBatch(BatchSize)',       C_BRONZE),
    (5.9, 1, 0, '→ batch: List<SalesRaw>',        C_BRONZE),
    (5.4, 0, 1, 'LoadAsync(batch)',                C_BRONZE),
    (4.9, 1, 4, 'INSERT stg.SalesRaw',             C_GRAY),
    (4.4, 1, 0, '← BronzeResult(Inserted)',        C_BRONZE),
    (3.9, 0, 4, 'StartRunAsync(rowsRead)',          C_PURPLE),
    (3.4, 0, 2, 'ProcessAsync()',                  C_SILVER),
    (3.0, 2, 4, 'AnyAsync() — dedup check',        C_GRAY),
    (2.6, 2, 0, '← SilverResult\n(+RejectedReasons)', C_SILVER),
    (2.1, 0, 4, 'LogAsync(Warn/Info)',             C_PURPLE),
    (1.6, 0, 3, 'LoadAsync()',                     '#A16207'),
    (1.1, 3, 4, 'INSERT dw.FactSales',             C_GRAY),
    (0.7, 3, 0, '← GoldResult(Inserted)',          '#A16207'),
    (0.3, 0, 4, 'FinishRunAsync(Success)',         C_PURPLE),
]

for (y, frm, to, txt, clr) in steps:
    x1, x2 = cx(frm), cx(to)
    dx = 0.15 if x2 > x1 else -0.15
    ax.annotate('', xy=(x2-dx, y), xytext=(x1+dx, y),
                arrowprops=dict(arrowstyle='->', color=clr, lw=1.4))
    mx = (x1+x2)/2
    ax.text(mx, y+0.12, txt, ha='center', va='bottom',
            fontsize=7.5, color=clr,
            bbox=dict(fc='white', ec='none', pad=1))

# Анімація
anim_rect = FancyBboxPatch((0.15, -0.05), 2.3, 0.55,
                           boxstyle='round,pad=0.04',
                           facecolor='#EDE9FE', edgecolor=C_PURPLE, lw=1.5, ls=':')
ax.add_patch(anim_rect)
ax.text(1.3, 0.22, 'AnimateBatchAsync\n(DelayMs / BatchSize)', ha='center',
        fontsize=7.5, color=C_PURPLE, style='italic')

ax.text(7, 0.0, 'Рисунок 3.3 — Послідовність виконання одного кроку ETL-пайплайну',
        ha='center', fontsize=9, color=C_GRAY, style='italic')

plt.tight_layout()
plt.savefig(os.path.join(OUT, 'fig3_3_sequence.png'), dpi=180, bbox_inches='tight')
plt.close()
print('fig3_3 done')

# ════════════════════════════════════════════════════════════════════════════
# Рис 3.6 — Named Pipe IPC
# ════════════════════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(12, 5))
ax.set_xlim(0, 12); ax.set_ylim(0, 5)
ax.axis('off')
fig.patch.set_facecolor('white')

ax.text(6, 4.7, 'Міжпроцесна взаємодія через Windows Named Pipe',
        ha='center', fontsize=13, fontweight='bold', color=C_GRAY)

# Web App box
box(ax, 0.3, 1.5, 3.2, 2.5, 'ETL_web_project', 'ASP.NET Core процес',
    fc=C_LGREEN, ec=C_GREEN, lw=2, fontsize=11, bold=True)
box(ax, 0.5, 2.2, 2.8, 0.55, 'EtlController\n.RunJob(jobId)', fc='white', ec=C_GREEN, fontsize=8.5)
box(ax, 0.5, 1.6, 2.8, 0.55, 'EtlJobService\n.TriggerRunAsync()', fc='white', ec=C_GREEN, fontsize=8.5)

# WPF box
box(ax, 8.5, 1.5, 3.2, 2.5, 'ETL_simulator', 'WPF процес',
    fc=C_LBLUE, ec=C_BLUE, lw=2, fontsize=11, bold=True)
box(ax, 8.7, 2.7, 2.8, 0.55, 'PipeListener\n(фоновий Task)', fc='white', ec=C_BLUE, fontsize=8.5)
box(ax, 8.7, 2.1, 2.8, 0.55, 'MainViewModel\n.TriggerExternalRun()', fc='white', ec=C_BLUE, fontsize=8.5)
box(ax, 8.7, 1.55, 2.8, 0.45, 'RunStepAsync(ct)', fc='white', ec=C_BLUE, fontsize=8.5)

# Pipe channel
pipe_rect = FancyBboxPatch((3.8, 2.1), 4.4, 1.4,
                           boxstyle='round,pad=0.08',
                           facecolor=C_LPURP, edgecolor=C_PURPLE, linewidth=2)
ax.add_patch(pipe_rect)
ax.text(6.0, 2.95, 'Named Pipe', ha='center', fontsize=11,
        fontweight='bold', color=C_PURPLE)
ax.text(6.0, 2.55, '"etl-wpf-simulator"', ha='center', fontsize=9,
        color=C_PURPLE, style='italic')
ax.text(6.0, 2.2, 'ConnectAsync(timeout=2000ms)', ha='center',
        fontsize=8, color='#6D28D9')

# Стрілки
ax.annotate('', xy=(3.8, 2.75), xytext=(3.3, 2.75),
            arrowprops=dict(arrowstyle='->', color=C_GREEN, lw=2))
ax.text(3.55, 2.95, '"RUN"', ha='center', fontsize=9,
        color=C_GREEN, fontweight='bold')

ax.annotate('', xy=(8.5, 2.75), xytext=(8.2, 2.75),
            arrowprops=dict(arrowstyle='->', color=C_BLUE, lw=2))

# Timeout path
ax.annotate('', xy=(1.9, 1.5), xytext=(1.9, 1.3),
            arrowprops=dict(arrowstyle='->', color=C_RED, lw=1.5))
box(ax, 0.5, 0.6, 2.8, 0.65, 'TimeoutException →\n"WPF не запущено"',
    fc=C_LRED, ec=C_RED, fontsize=8)
ax.text(0.55, 1.35, 'якщо timeout', fontsize=8, color=C_RED, style='italic')

# Dispatcher
ax.annotate('', xy=(9.1, 2.1), xytext=(9.1, 2.7),
            arrowprops=dict(arrowstyle='->', color='#9CA3AF', lw=1.5))
ax.text(9.0, 2.38, 'Dispatcher\n.Invoke', fontsize=7.5,
        color='#6B7280', ha='right', style='italic')

ax.text(6, 0.15, 'Рисунок 3.6 — Схема міжпроцесної взаємодії через Named Pipe',
        ha='center', fontsize=9, color=C_GRAY, style='italic')

plt.tight_layout()
plt.savefig(os.path.join(OUT, 'fig3_6_namedpipe.png'), dpi=180, bbox_inches='tight')
plt.close()
print('fig3_6 done')
print('Всі рисунки збережено у', OUT)
